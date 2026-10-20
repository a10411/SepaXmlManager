using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IUploadService
    {
        Task<int> ProcessCsvFileAsync(IFormFile file, string operationType);
    }
}
