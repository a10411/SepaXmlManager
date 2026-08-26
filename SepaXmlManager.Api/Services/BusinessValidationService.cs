using SepaXmlManager.Api.Services.Interfaces;
using System.Numerics;
using VatValidation;

namespace SepaXmlManager.Api.Services
{
    public class BusinessValidationService:IBusinessValidationService
    {
        public bool IsIbanValid(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;


            iban = iban.Replace(" ", "").ToUpper();
            if (iban.Length < 15 || iban.Length > 34) return false;

            //Move os primeiros 4 caracteres para o fim da string
            string rearrangedIban = iban.Substring(4) + iban.Substring(0, 4);
            string numericIban = "";
            foreach (char c in rearrangedIban)
            {
                if (char.IsLetter(c))
                {
                    // Truque de C#: subtrair 'A' (65) e somar 10 dá o valor exato!
                    numericIban += (c - 'A' + 10).ToString();
                }
                else if (char.IsDigit(c))
                {
                    numericIban += c;
                }
                else
                {
                    return false; // Se tiver símbolos estranhos, chumba logo.
                }
            }

            if (BigInteger.TryParse(numericIban, out BigInteger ibanNumber))
            {
                return (ibanNumber % 97) == 1;
            }

            return false;
        }

        public bool IsValidTaxId(string countryCode, string taxId)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(taxId))
            {
                return false;
            }

            try
            {
                countryCode = countryCode.Trim().ToUpper();
                taxId = taxId.Trim();

                return VatNumber.TryParse($"{countryCode}{taxId}", out _);
            }
            catch
            {
                return false;
            }
        }
    }
}
