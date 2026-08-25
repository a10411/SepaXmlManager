using SepaXmlManager.Models.Entities;
using ProjetoFinal.Models;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface ICompanyService
    {
  
        Task<Company> GetCompanyAsync();

        Task<Company> SaveCompanyAsync(Company company);
        
    }
}
