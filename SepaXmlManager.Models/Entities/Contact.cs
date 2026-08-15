using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entities
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; } // Foreign Key

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(34)]
        public string IBAN { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [MaxLength(11)]
        public string BIC { get; set; } = string.Empty;

        [MaxLength(35)]
        public string? MandateReference { get; set; } = string.Empty;// Para pain.008

        public DateTime? DateSignatureMandate { get; set; } // Para pain.008

        // Relacionamentos
        public Company Company { get; set; } = null!;
        public List<Transaction> Transaction { get; set; } = new List<Transaction>();
    }
}
