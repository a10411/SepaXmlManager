using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IUploadService
    {
        // Recebe um ficheiro (IFormFile) do utilizador e diz-nos se era Transferência (pain.001) ou Cobrança (pain.008)
        Task<int> ProcessCsvFileAsync(IFormFile file, string operationType);
    }
}
