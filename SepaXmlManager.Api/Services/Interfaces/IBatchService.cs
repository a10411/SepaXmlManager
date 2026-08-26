using SepaXmlManager.Models.Entities; 
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IBatchService
    {
        Task<TransferBatch> CreateBatchAsync(TransferBatch batch);
        Task<TransferBatch> GetBatchByIdAsync(int id);
        Task<IEnumerable<TransferBatch>> GetAllBatchesAsync();
        Task<Transaction> AddTransactionToBatchAsync(int batchId, Transaction transaction);
    }
}