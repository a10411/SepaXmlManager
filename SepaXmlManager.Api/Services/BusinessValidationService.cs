using SepaXmlManager.Api.Services.Interfaces;
using System.Numerics;
using VatValidation;

namespace SepaXmlManager.Api.Services
{
    public class BusinessValidationService : IBusinessValidationService
    {
        /// <summary>
        /// Valida a integridade estrutural e matemática de um número de conta bancária no formato IBAN (International Bank Account Number).
        /// </summary>
        /// <param name="iban">A string alfanumérica que representa o IBAN a ser validado.</param>
        /// <returns>Retorna verdadeiro (true) caso o IBAN cumpra a norma do Módulo 97, ou falso (false) caso seja inválido ou contenha formatação incorreta.</returns>
        public bool IsIbanValid(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;

            // Normaliza a string, removendo espaços e convertendo para maiúsculas.
            iban = iban.Replace(" ", "").ToUpper();
            if (iban.Length < 15 || iban.Length > 34) return false;

            // Move os primeiros 4 caracteres para o final da string de acordo com a norma internacional de validação.
            string rearrangedIban = iban.Substring(4) + iban.Substring(0, 4);
            string numericIban = "";

            foreach (char c in rearrangedIban)
            {
                if (char.IsLetter(c))
                {
                    // Converte caracteres alfabéticos para o seu equivalente numérico (onde A = 10, B = 11, etc.).
                    numericIban += (c - 'A' + 10).ToString();
                }
                else if (char.IsDigit(c))
                {
                    numericIban += c;
                }
                else
                {
                    // Interrompe a validação e retorna falso caso seja detetado um caractere não alfanumérico.
                    return false;
                }
            }

            // Efetua a verificação matemática do Módulo 97 estipulada para a validação de IBANs.
            if (BigInteger.TryParse(numericIban, out BigInteger ibanNumber))
            {
                return (ibanNumber % 97) == 1;
            }

            return false;
        }

        /// <summary>
        /// Efetua a validação de um Número de Identificação Fiscal (NIF/VAT) de acordo com as regras matemáticas do país de origem.
        /// </summary>
        /// <param name="countryCode">O código ISO de dois caracteres correspondente ao país emissor (ex: "PT").</param>
        /// <param name="taxId">O número de identificação fiscal a ser submetido a validação.</param>
        /// <returns>Retorna verdadeiro (true) se o número fiscal for válido para a jurisdição especificada, caso contrário, retorna falso (false).</returns>
        public bool IsValidTaxId(string countryCode, string taxId)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(taxId))
            {
                return false;
            }

            try
            {
                // Normaliza os dados de entrada antes da validação.
                countryCode = countryCode.Trim().ToUpper();
                taxId = taxId.Trim();

                // Recorre à biblioteca de validação para confirmar a validade do NIF/VAT associado ao código do país.
                return VatNumber.TryParse($"{countryCode}{taxId}", out _);
            }
            catch
            {
                return false;
            }
        }
    }
}