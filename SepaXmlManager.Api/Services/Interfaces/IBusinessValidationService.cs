namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IBusinessValidationService
    {
        bool IsIbanValid(string iban);
    }
}
