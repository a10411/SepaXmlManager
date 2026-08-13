using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entidades
{
    public class Empresa
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string NIF { get; set; } = string.Empty;

        [Required, MaxLength(34)]
        public string IBAN { get; set; } = string.Empty;

        [Required, MaxLength(11)]
        public string BIC { get; set; } = string.Empty;

        [MaxLength(35)]
        public string IdentificadorCredor { get; set; } = string.Empty; // Opcional, mas obrigatório para pain.008

        // Relacionamentos (1 Empresa tem Muitos Lotes e Contactos)
        public List<Contacto> Contactos { get; set; } = new List<Contacto>();
        public List<LoteTransferencia> Lotes { get; set; } = new List<LoteTransferencia>();
    }
}
