using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models;
using SepaXmlManager.Models.DTO;
using SepaXmlManager.Models.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Services
{
    public class UploadService : IUploadService
    {
        private readonly AppDbContext _context;
        private readonly ICompanyService _companyService;
        private readonly IBusinessValidationService _validationService;

        // Injeta as dependências do contexto da base de dados, do serviço de gestão de empresas e do serviço de validação de negócio.
        public UploadService(AppDbContext context, ICompanyService companyService, IBusinessValidationService validationService)
        {
            _context = context;
            _companyService = companyService;
            _validationService = validationService;
        }

        /// <summary>
        /// Processa a importação de um ficheiro CSV contendo transações financeiras, procedendo à criação automática do lote e dos registos de contactos inexistentes.
        /// </summary>
        /// <param name="file">O fluxo de dados do ficheiro CSV submetido.</param>
        /// <param name="operationType">A natureza da operação financeira a processar (ex: "PAGAMENTO" ou "COBRANCA").</param>
        /// <returns>Retorna o identificador numérico único (ID) do lote criado em base de dados.</returns>
        /// <exception cref="ArgumentException">Lançada caso o ficheiro seja inválido, vazio, não seja CSV ou possua dados inconsistentes na sua estrutura.</exception>
        /// <exception cref="InvalidOperationException">Lançada caso a entidade emissora não esteja previamente configurada no sistema.</exception>
        public async Task<int> ProcessCsvFileAsync(IFormFile file, string operationType)
        {
            // Efetua as verificações de integridade e formato do ficheiro submetido.
            if (file == null || file.Length == 0)
                throw new ArgumentException("O ficheiro está vazio ou não foi enviado.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Apenas ficheiros .csv são suportados nesta versão.");

            var company = await _companyService.GetCompanyAsync();
            if (company == null)
                throw new InvalidOperationException("Configure a Empresa antes de importar ficheiros.");

            // Inicializa o processo de leitura e extração de dados utilizando a biblioteca CsvHelper.
            var records = new List<CsvTransactionDto>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";", // Configura o delimitador padrão (ponto e vírgula) aplicável à generalidade dos ficheiros gerados regionalmente.
                HasHeaderRecord = true,
                MissingFieldFound = null // Ignora campos em falta durante o mapeamento, garantindo a tolerância a variações estruturais no ficheiro de origem.
            };

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, config))
            {
                // Transpõe o conteúdo estruturado do ficheiro para uma coleção em memória.
                records = csv.GetRecords<CsvTransactionDto>().ToList();
            }

            if (!records.Any())
                throw new ArgumentException("O ficheiro CSV não tem linhas válidas.");

            // Instancia a entidade de lote principal (Batch) para enquadrar as transações importadas.
            var batch = new TransferBatch
            {
                CompanyId = company.Id,
                DateCreation = DateTime.Now,
                TypeOperation = operationType.ToUpper() == "COBRANCA" ? "PAIN.008" : "PAIN.001",
                MsgId = $"MSG-{DateTime.Now:yyyyMMddHHmmss}",
                TotalTransactions = records.Count,
                TotalAmount = 0m // O valor financeiro total será apurado cumulativamente durante a iteração das transações.
            };

            _context.TransferBatches.Add(batch);
            await _context.SaveChangesAsync(); // Consolida a gravação para obtenção de um identificador real na base de dados.

            // Inicia o processamento individual de cada registo extraído.
            decimal totalBatchAmount = 0;
            int counter = 1;

            foreach (var record in records)
            {
                // Normaliza os identificadores e formatos, removendo espaços e assegurando a capitalização correta.
                string cleanIban = record.IBAN.Replace(" ", "").ToUpper();
                string cleanNif = record.NIF.Replace(" ", "");

                // Efetua a extração e conversão do valor financeiro, ignorando eventuais símbolos monetários.
                if (!decimal.TryParse(record.Valor.Replace("€", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmount))
                    throw new ArgumentException($"Valor inválido '{record.Valor}' na linha {counter}.");

                // Valida a conformidade matemática do IBAN perante as normas internacionais (Módulo 97).
                if (!_validationService.IsIbanValid(cleanIban))
                    throw new ArgumentException($"O IBAN '{cleanIban}' da linha {counter} é inválido!");

                // Verifica a preexistência do contacto na base de dados recorrendo ao IBAN como chave primária de pesquisa de negócio.
                var existingContact = await _context.Contacts.FirstOrDefaultAsync(c => c.IBAN == cleanIban);

                if (existingContact == null)
                {
                    // Instancia e persiste um novo contacto caso não seja detetado um registo prévio no sistema.
                    existingContact = new Contact
                    {
                        CompanyId = company.Id,
                        Name = string.IsNullOrWhiteSpace(record.Nome) ? "Cliente Desconhecido" : record.Nome,
                        IBAN = cleanIban,
                        NIF = cleanNif,
                        BIC = "XXXPTPL0",
                        IsActive = true,
                        MandateReference = operationType.ToUpper() == "COBRANCA" ? $"MND-{DateTime.Now:yyyyMM}" : ""
                    };

                    _context.Contacts.Add(existingContact);
                    await _context.SaveChangesAsync();
                }

                // Cria a transação associando as respetivas entidades hierárquicas (Lote e Contacto).
                var transaction = new Transaction
                {
                    TransferBatchId = batch.Id,
                    ContactId = existingContact.Id,
                    EndToEndId = $"E2E-{batch.Id}-{counter:D3}", // Gera um identificador end-to-end único e padronizado sequencialmente.
                    Amount = parsedAmount,
                    Description = string.IsNullOrWhiteSpace(record.Descricao) ? "Processamento Automático" : record.Descricao
                };

                _context.Transactions.Add(transaction);

                totalBatchAmount += parsedAmount;
                counter++;
            }

            // Atualiza o montante financeiro total agregado do lote processado.
            batch.TotalAmount = totalBatchAmount;

            // Efetiva a persistência final de todas as transações agregadas na base de dados.
            await _context.SaveChangesAsync();

            // Retorna o identificador do lote instanciado para referência e monitorização na camada de controlo.
            return batch.Id;
        }
    }
}