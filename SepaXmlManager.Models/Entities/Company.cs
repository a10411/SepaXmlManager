using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entidades
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string NIF { get; set; } = string.Empty;

        [Required, MaxLength(34)]
        public string IBAN { get; set; } = string.Empty;

        [Required, MaxLength(11)]
        public string BIC { get; set; } = string.Empty;

        [MaxLength(35)]
        public string IdentityCreditor { get; set; } = string.Empty; // Opcional, mas obrigatório para pain.008

        // Relacionamentos (1 Empresa tem Muitos Lotes e Contactos)
        public List<Contacts> Contacts { get; set; } = new List<Contacts>();
        public List<TransferBatch> Batches { get; set; } = new List<TransferBatch>();
    }
}
