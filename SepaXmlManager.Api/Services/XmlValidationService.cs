using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using SepaXmlManager.Api.Services.Interfaces;
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
                    result.Errors.Add($"O tipo de documento '{documentType}' não é suportado. Use 'PAIN001' ou 'PAIN008'.");
                    return result;
            }

            // 2. Construir o caminho exato para Schemas
            string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", schemaFileName);

            if (!File.Exists(schemaPath))
            {
                result.IsValid = false;
                result.Errors.Add($"O ficheiro de regras do banco (XSD) não foi encontrado no servidor: {schemaPath}");
                return result;
            }

            // 3. Configurar XmlReaderSettings
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema
            };

            settings.Schemas.Add(null, schemaPath);

            // A MÁGICA ACONTECE AQUI: Quando o leitor encontra um erro, passa pelo tradutor!
            settings.ValidationEventHandler += (sender, e) =>
            {
                result.IsValid = false;

                // Chamamos o nosso novo tradutor de erros humano antes de guardar
                string humanMessage = TranslateXsdError(e.Message);

                result.Errors.Add($"[Linha {e.Exception?.LineNumber}] {humanMessage}");
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
                result.Errors.Add($"O ficheiro XML está corrompido ou tem um formato inválido. Detalhes: {ex.Message}");
            }

            return result;
        }

        // --- NOVO MÉTODO: O Dicionário Tradutor Humano ---
        private string TranslateXsdError(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return "Erro desconhecido de formatação ou validação.";

            // 1. Traduzir erros do Mandato (MndtId)
            if (rawMessage.Contains(":MndtId") && rawMessage.Contains("MinLength"))
            {
                return "A Referência do Mandato (Mandate Reference) não pode estar vazia num ficheiro de Débitos Diretos.";
            }
            if (rawMessage.Contains(":MndtId") && rawMessage.Contains("MaxLength"))
            {
                return "A Referência do Mandato excede o limite máximo permitido de 35 caracteres.";
            }

            // 2. Traduzir erros do IBAN
            if (rawMessage.Contains(":IBAN") && (rawMessage.Contains("Pattern") || rawMessage.Contains("datatype")))
            {
                return "Um dos IBANs presentes no ficheiro tem um formato inválido.";
            }

            // 3. Traduzir erros de Valores/Montantes (InstdAmt ou CtrlSum)
            if ((rawMessage.Contains(":InstdAmt") || rawMessage.Contains(":CtrlSum")) && rawMessage.Contains("FractionDigits"))
            {
                return "Um dos montantes tem demasiadas casas decimais. Os valores devem ter formato padrão (ex: 10.50).";
            }

            // 4. Traduzir erros de Nomes vazios
            if (rawMessage.Contains(":Nm") && rawMessage.Contains("MinLength"))
            {
                return "Existe um Nome de Cliente ou Empresa que se encontra vazio.";
            }

            // 5. Traduzir erros do BIC
            if (rawMessage.Contains(":BICFI") && rawMessage.Contains("Pattern"))
            {
                return "Um código BIC/SWIFT não cumpre as regras internacionais de formatação (deve ter 8 ou 11 caracteres).";
            }

            // Fallback: Se não encontrarmos a tradução, devolvemos a mensagem técnica original
            // para que nós (programadores) ainda possamos fazer debug se necessário.
            return $"Erro de formatação SEPA: {rawMessage}";
        }
    }
}