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

        public UploadService(AppDbContext context, ICompanyService companyService, IBusinessValidationService validationService)
        {
            _context = context;
            _companyService = companyService;
            _validationService = validationService;
        }

        public async Task<int> ProcessCsvFileAsync(IFormFile file, string operationType)
        {
            // 1. Verificações de Segurança Básicas
            if (file == null || file.Length == 0)
                throw new ArgumentException("O ficheiro está vazio ou não foi enviado.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Apenas ficheiros .csv são suportados nesta versão.");

            var company = await _companyService.GetCompanyAsync();
            if (company == null)
                throw new InvalidOperationException("Configure a Empresa antes de importar ficheiros.");

            // 2. Leitura do Ficheiro com CsvHelper
            var records = new List<CsvTransactionDto>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";", // Normalmente em Portugal os CSVs vêm com ponto e vírgula
                HasHeaderRecord = true,
                MissingFieldFound = null // Não bloqueia se faltar alguma coluna extra
            };

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, config))
            {
                // Converte todas as linhas do CSV diretamente para a nossa lista!
                records = csv.GetRecords<CsvTransactionDto>().ToList();
            }

            if (!records.Any())
                throw new ArgumentException("O ficheiro CSV não tem linhas válidas.");

            // 3. Criar o Lote Principal (Batch)
            var batch = new TransferBatch
            {
                CompanyId = company.Id,
                DateCreation = DateTime.Now,
                TypeOperation = operationType.ToUpper() == "COBRANCA" ? "PAIN.008" : "PAIN.001",
                MsgId = $"MSG-{DateTime.Now:yyyyMMddHHmmss}",
                TotalTransactions = records.Count,
                TotalAmount = 0m // Vamos calcular isto já a seguir
            };

            _context.TransferBatches.Add(batch);
            await _context.SaveChangesAsync(); // Guarda para termos um ID de Lote verdadeiro

            // 4. Processar cada linha do Excel/CSV
            decimal totalBatchAmount = 0;
            int counter = 1;

            foreach (var record in records)
            {
                // Limpar espaços nos IBANs (o utilizador costuma copiar com espaços do Excel)
                string cleanIban = record.IBAN.Replace(" ", "").ToUpper();
                string cleanNif = record.NIF.Replace(" ", "");

                // Validar montante (ignorar o "€" se houver)
                if (!decimal.TryParse(record.Valor.Replace("€", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmount))
                    throw new ArgumentException($"Valor inválido '{record.Valor}' na linha {counter}.");

                // Validar se IBAN é matematicamente possível (Módulo 97)
                if (!_validationService.IsIbanValid(cleanIban))
                    throw new ArgumentException($"O IBAN '{cleanIban}' da linha {counter} é inválido!");

                // Verificar se o cliente já existe na Base de Dados (procuramos pelo IBAN)
                var existingContact = await _context.Contacts.FirstOrDefaultAsync(c => c.IBAN == cleanIban);

                if (existingContact == null)
                {
                    // Se não existe, a API cria automaticamente o cliente no sistema!
                    existingContact = new Contact
                    {
                        CompanyId = company.Id,
                        Name = string.IsNullOrWhiteSpace(record.Nome) ? "Cliente Desconhecido" : record.Nome,
                        IBAN = cleanIban,
                        NIF = cleanNif,
                        BIC = "XXXPTPL0", // Um BIC dummy caso o CSV não tenha. O ideal é ter a validação desativada se o Banco não exigir.
                        IsActive = true,
                        MandateReference = operationType.ToUpper() == "COBRANCA" ? $"MND-{DateTime.Now:yyyyMM}" : ""
                    };

                    _context.Contacts.Add(existingContact);
                    await _context.SaveChangesAsync(); // Guarda para termos o ID do Contacto
                }

                // Finalmente, criamos a transação e ligamos ao Lote e ao Cliente!
                var transaction = new Transaction
                {
                    TransferBatchId = batch.Id,
                    ContactId = existingContact.Id,
                    EndToEndId = $"E2E-{batch.Id}-{counter:D3}", // Cria um EndToEndId único e bonito
                    Amount = parsedAmount,
                    Description = string.IsNullOrWhiteSpace(record.Descricao) ? "Processamento Automático" : record.Descricao
                };

                _context.Transactions.Add(transaction);

                totalBatchAmount += parsedAmount;
                counter++;
            }

            // 5. Atualiza o Total de Dinheiro do Lote
            batch.TotalAmount = totalBatchAmount;

            // Grava o resto no SQL
            await _context.SaveChangesAsync();

            // Retorna o ID do Lote criado para que o Controlador possa usá-lo se quiser
            return batch.Id;
        }
    }
}