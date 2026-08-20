using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entities
{
    public class TransferBatch
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; } // Foreign Key

        [Required, MaxLength(35)]
        public string MsgId { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        [Required, MaxLength(10)]
        public string TypeOperation { get; set; } = string.Empty;// "PAIN.001" ou "PAIN.008"

        public int TotalTransactions { get; set; }

        public decimal TotalAmount { get; set; }

        public string FilePath { get; set; } = string.Empty;

        

        // Relacionamentos
        public Company Company { get; set; } = null!;
        public List<Transaction> Transaction { get; set; } = new List<Transaction>();
    }
}
