

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IBusinessValidationService
    {
        bool IsIbanValid(string iban);
        bool IsValidTaxId(string countryCode, string taxId);
    }
}
