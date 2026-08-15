using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entities
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        public int TransferBatchId { get; set; }
        public int ContactId { get; set; }

        [Required, MaxLength(35)]
        public string EndToEndId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [MaxLength(140)]
        public string Description { get; set; } = string.Empty;

        public TransferBatch TransferBatch { get; set; } = null!;
        public Contact Contact { get; set; } = null!;
    }
}