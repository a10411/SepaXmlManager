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

        public BatchService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TransferBatch> CreateBatchAsync(TransferBatch batch)
        {
            var myCompany = await _context.Companies.FirstOrDefaultAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Error: You must configure your company in the system first!");
            }

            batch.CompanyId = myCompany.Id;

            // 2. Garantir que os totais começam limpos a zeros
            batch.TotalAmount = 0;
            batch.TotalTransactions = 0;

            if (batch.DateCreation == default)
            {
                batch.DateCreation = DateTime.Now;
            }

            // 3. A NOSSA ALTERAÇÃO INTELIGENTE: Auto-gerar o MsgId se vier em branco
            if (string.IsNullOrWhiteSpace(batch.MsgId))
            {
                // Gera um ID único com base na data/hora, ex: BATCH-20260825-172101
                batch.MsgId = $"BATCH-{DateTime.Now:yyyyMMdd-HHmmss}";
            }

            // 4. Guardar na Base de Dados
            _context.TransferBatches.Add(batch);
            await _context.SaveChangesAsync();

            return batch;
        }
        public async Task<TransferBatch> GetBatchByIdAsync(int id)
        {
        
            //Pedimos as transferBatches mas também as transações e contactos
            return await _context.TransferBatches
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<TransferBatch>> GetAllBatchesAsync()
        {
            return await _context.TransferBatches.ToListAsync();
        }


        public async Task<Transaction> AddTransactionToBatchAsync(int batchId, Transaction transaction)
        {
            // 1. Verificar se o lote existe
            var batch = await _context.TransferBatches.FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null)
            {
                throw new KeyNotFoundException($"Batch with id {batchId} was not found.");
            }

            // Verifica se o contacto existe
            var contactExists = await _context.Contacts.AnyAsync(c => c.Id == transaction.ContactId);
            if (!contactExists)
            {
                throw new ArgumentException($"Contact with id {transaction.ContactId} does not exist in the data base!");
            }

            // associa cada transação ao lote
            transaction.TransferBatchId = batchId;
            _context.Transactions.Add(transaction);

            
            //soma cada transação ao total, para controlo
            batch.TotalAmount += transaction.Amount;
            batch.TotalTransactions += 1;

            await _context.SaveChangesAsync();

            return transaction;
        }

    }
}
