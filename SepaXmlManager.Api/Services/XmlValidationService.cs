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
        /// <summary>
        /// Efetua a validação estrutural de um fluxo de dados XML contra os esquemas XSD oficiais (SEPA pain.001 ou pain.008).
        /// </summary>
        /// <param name="xmlStream">O fluxo de dados (Stream) contendo o conteúdo do ficheiro XML a analisar.</param>
        /// <param name="documentType">A designação do standard esperado (ex: "PAIN001" ou "PAIN008").</param>
        /// <returns>Retorna um objeto XmlValidationResult indicando o sucesso da operação ou detalhando as anomalias detetadas em formato legível.</returns>
        public XmlValidationResult ValidateXml(Stream xmlStream, string documentType)
        {
            var result = new XmlValidationResult { IsValid = true };
            string schemaFileName = string.Empty;

            // Identifica o ficheiro XSD aplicável com base no tipo de documento (Transferências ou Cobranças).
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

            // Constrói o caminho físico absoluto para aceder ao ficheiro de validação (Schema).
            string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", schemaFileName);

            if (!File.Exists(schemaPath))
            {
                result.IsValid = false;
                result.Errors.Add($"O ficheiro de regras do banco (XSD) não foi encontrado no servidor: {schemaPath}");
                return result;
            }

            // Configura as definições de leitura para incluir a validação por Schema.
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema
            };

            settings.Schemas.Add(null, schemaPath);

            // Regista o evento para intercetar falhas de validação durante a leitura do XML.
            settings.ValidationEventHandler += (sender, e) =>
            {
                result.IsValid = false;

                // Invoca o tradutor para converter a mensagem técnica numa mensagem percetível.
                string humanMessage = TranslateXsdError(e.Message);

                result.Errors.Add($"[Linha {e.Exception?.LineNumber}] {humanMessage}");
            };

            // Processa o ficheiro XML de forma iterativa.
            try
            {
                using (var reader = XmlReader.Create(xmlStream, settings))
                {
                    while (reader.Read())
                    {
                        // A leitura percorre o ficheiro. O ValidationEventHandler é acionado de forma automática em caso de anomalia estrutural.
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

        /// <summary>
        /// Método auxiliar responsável por mapear os erros técnicos complexos do validador XSD para mensagens de utilizador descritivas.
        /// </summary>
        /// <param name="rawMessage">A mensagem de erro técnica original gerada pelo motor de validação XML.</param>
        /// <returns>Retorna uma string traduzida contendo o erro em linguagem de negócio.</returns>
        private string TranslateXsdError(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return "Erro desconhecido de formatação ou validação.";

            // Traduz erros associados à referência do mandato (MndtId).
            if (rawMessage.Contains(":MndtId") && rawMessage.Contains("MinLength"))
            {
                return "A Referência do Mandato (Mandate Reference) não pode estar vazia num ficheiro de Débitos Diretos.";
            }
            if (rawMessage.Contains(":MndtId") && rawMessage.Contains("MaxLength"))
            {
                return "A Referência do Mandato excede o limite máximo permitido de 35 caracteres.";
            }

            // Traduz erros de formatação no IBAN.
            if (rawMessage.Contains(":IBAN") && (rawMessage.Contains("Pattern") || rawMessage.Contains("datatype")))
            {
                return "Um dos IBANs presentes no ficheiro tem um formato inválido.";
            }

            // Traduz erros associados ao número de casas decimais dos montantes (InstdAmt ou CtrlSum).
            if ((rawMessage.Contains(":InstdAmt") || rawMessage.Contains(":CtrlSum")) && rawMessage.Contains("FractionDigits"))
            {
                return "Um dos montantes tem demasiadas casas decimais. Os valores devem ter formato padrão (ex: 10.50).";
            }

            // Traduz erros de preenchimento obrigatório para nomes.
            if (rawMessage.Contains(":Nm") && rawMessage.Contains("MinLength"))
            {
                return "Existe um Nome de Cliente ou Empresa que se encontra vazio.";
            }

            // Traduz erros de formatação do código BIC/SWIFT.
            if (rawMessage.Contains(":BICFI") && rawMessage.Contains("Pattern"))
            {
                return "Um código BIC/SWIFT não cumpre as regras internacionais de formatação (deve ter 8 ou 11 caracteres).";
            }

            // Retorna a mensagem técnica original caso não exista uma tradução mapeada, garantindo que o erro não é perdido para análise técnica.
            return $"Erro de formatação SEPA: {rawMessage}";
        }
    }
}