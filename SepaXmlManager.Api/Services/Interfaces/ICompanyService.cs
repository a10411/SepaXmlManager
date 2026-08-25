using SepaXmlManager.Models.Entities;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface ICompanyService
    {
        public interface ICompanyService
        {

            Task<Company> GetCompanyAsync();

            Task<Company> SaveCompanyAsync(Company company);
        }
    }
}
