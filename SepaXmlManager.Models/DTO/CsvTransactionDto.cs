using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.DTO
{
    public class CsvTransactionDto
    {
        public string Nome { get; set; } = string.Empty;

        public string IBAN { get; set; } = string.Empty;

        public string NIF { get; set; } = string.Empty;

        public string Valor { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;
    }
}
