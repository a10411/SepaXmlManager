using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Services
{
    public class BatchService : IBatchService
    {
        private readonly AppDbContext _context;

        // Injeta a dependência do contexto da base de dados.
        public BatchService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria um novo lote de transferências ou cobranças, validando a configuração da empresa emissora e inicializando as métricas de controlo.
        /// </summary>
        /// <param name="batch">A entidade contendo os dados iniciais do lote a criar.</param>
        /// <returns>Retorna o lote gerado e persistido na base de dados.</returns>
        public async Task<TransferBatch> CreateBatchAsync(TransferBatch batch)
        {
            // Valida a existência prévia da empresa configurada no sistema antes de prosseguir com a criação do lote.
            var myCompany = await _context.Companies.FirstOrDefaultAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Error: You must configure your company in the system first!");
            }

            // Associa o identificador da empresa emissora ao lote.
            batch.CompanyId = myCompany.Id;

            // Garante a inicialização das variáveis numéricas de controlo a zero.
            batch.TotalAmount = 0;
            batch.TotalTransactions = 0;

            // Define a data atual como data de criação, caso nenhuma data seja explicitamente providenciada.
            if (batch.DateCreation == default)
            {
                batch.DateCreation = DateTime.Now;
            }

            // Gera automaticamente um identificador único para a mensagem (MsgId) caso o campo não seja fornecido pelo utilizador.
            if (string.IsNullOrWhiteSpace(batch.MsgId))
            {
                // O formato do identificador baseia-se num prefixo padronizado concatenado à data e hora do sistema.
                batch.MsgId = $"BATCH-{DateTime.Now:yyyyMMdd-HHmmss}";
            }

            // Persiste a nova entidade de lote na base de dados.
            _context.TransferBatches.Add(batch);
            await _context.SaveChangesAsync();

            return batch;
        }

        /// <summary>
        /// Obtém os detalhes de um lote específico, incluindo toda a hierarquia de transações e contactos associados.
        /// </summary>
        /// <param name="id">O identificador numérico único do lote a recuperar.</param>
        /// <returns>Retorna a entidade do lote correspondente ou nulo caso não exista.</returns>
        public async Task<TransferBatch> GetBatchByIdAsync(int id)
        {
            // Retorna o lote especificado, carregando de forma antecipada (Eager Loading) a hierarquia de transações e os respetivos contactos associados.
            return await _context.TransferBatches
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        /// <summary>
        /// Obtém a listagem completa de todos os lotes registados na base de dados.
        /// </summary>
        /// <returns>Retorna uma coleção contendo todos os lotes de transferências e cobranças.</returns>
        public async Task<IEnumerable<TransferBatch>> GetAllBatchesAsync()
        {
            return await _context.TransferBatches.ToListAsync();
        }

        /// <summary>
        /// Regista uma nova transação financeira e associa-a a um lote existente, recalculando os totais de controlo do lote.
        /// </summary>
        /// <param name="batchId">O identificador numérico do lote alvo.</param>
        /// <param name="transaction">O objeto com os dados da transação a ser adicionada.</param>
        /// <returns>Retorna a transação processada após a consolidação com a base de dados.</returns>
        public async Task<Transaction> AddTransactionToBatchAsync(int batchId, Transaction transaction)
        {
            // Valida a existência do lote de transferências correspondente.
            var batch = await _context.TransferBatches.FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null)
            {
                throw new KeyNotFoundException($"Batch with id {batchId} was not found.");
            }

            // Confirma a existência do contacto referenciado na transação.
            var contactExists = await _context.Contacts.AnyAsync(c => c.Id == transaction.ContactId);
            if (!contactExists)
            {
                throw new ArgumentException($"Contact with id {transaction.ContactId} does not exist in the data base!");
            }

            // Associa a transação validada ao lote.
            transaction.TransferBatchId = batchId;
            _context.Transactions.Add(transaction);

            // Atualiza os contadores e montantes do lote principal de forma a refletir a adição da nova transação.
            batch.TotalAmount += transaction.Amount;
            batch.TotalTransactions += 1;

            // Submete as alterações combinadas à base de dados.
            await _context.SaveChangesAsync();

            return transaction;
        }
    }
}