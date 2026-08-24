using System.IO;
using SepaXmlManager.Models;

namespace SepaXmlManager.Api.Services
{
    public interface IXmlValidationService
    {
        XmlValidationResult ValidateXml(Stream xmlStream, string documentType);
    }
}
