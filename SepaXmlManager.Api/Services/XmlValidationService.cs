using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using SepaXmlManager.Models;

namespace SepaXmlManager.Api.Services
{
    public class XmlValidationService : IXmlValidationService
    {
        public XmlValidationResult ValidateXml(Stream xmlStream, string documentType)
        {
            var result = new XmlValidationResult { IsValid = true };
            string schemaFileName = string.Empty;

            // 1. Identificar qual é o ficheiro XSD a usar (Transferências ou Cobranças)
            switch (documentType.ToUpper())
            {
                case "PAIN001":
                    schemaFileName = "pain.001.001.09.xsd";
                    break;
                case "PAIN008":
                    schemaFileName = "pain.008.001.08.xsd";
                    break;
                default:
                    result.IsValid = false;
                    result.Errors.Add($"This type of file '{documentType}' is not supported. Use 'PAIN001' or 'PAIN008'.");
                    return result;
            }

            // 2. Construir o caminho exato para Schemas
            string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", schemaFileName);

            if (!File.Exists(schemaPath))
            {
                result.IsValid = false;
                result.Errors.Add($"Bank's rule file not found in server: {schemaPath}");
                return result;
            }

            // 3. Configures XmlReaderSettings
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema
            };

            settings.Schemas.Add(null, schemaPath);

            // Se o leitor encontrar um erro no ficheiro, guarda a mensagem na nossa lista
            settings.ValidationEventHandler += (sender, e) =>
            {
                result.IsValid = false;
                result.Errors.Add($"[Error in line -> {e.Exception?.LineNumber}] {e.Message}");
            };

            // 4. Ler o ficheiro XML linha a linha
            try
            {
                using (var reader = XmlReader.Create(xmlStream, settings))
                {
                    while (reader.Read())
                    {
                        // O leitor varre o ficheiro todo. Se houver erro de regras, o EventHandler dispara.
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Xml file is corrupted or with wrong format. Details: {ex.Message}");
            }

            return result;
        }
    }
}
