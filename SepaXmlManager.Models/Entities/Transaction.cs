using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entidades
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        public int TransferBatchID { get; set; } // Foreign Key
        public int ContactID { get; set; } // Foreign Key

        [Required, MaxLength(35)]
        public string EndToEndId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [MaxLength(140)]
        public string Description { get; set; } = string.Empty;

        // Relacionamentos
        public TransferBatch TransferBatch { get; set; } = null!;
        public Contacts Contact { get; set; } = null!;
    }
}
