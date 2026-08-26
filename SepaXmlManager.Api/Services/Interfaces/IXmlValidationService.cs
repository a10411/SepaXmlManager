using System.IO;
using SepaXmlManager.Models;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IXmlValidationService
    {
        XmlValidationResult ValidateXml(Stream xmlStream, string documentType);

    }
}
