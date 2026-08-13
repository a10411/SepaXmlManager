using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entidades
{
    public class Transacao
    {
        [Key]
        public int Id { get; set; }

        public int LoteTransferenciaId { get; set; } // Foreign Key
        public int ContactoId { get; set; } // Foreign Key

        [Required, MaxLength(35)]
        public string EndToEndId { get; set; } = string.Empty;

        public decimal Montante { get; set; }

        [MaxLength(140)]
        public string Descricao { get; set; } = string.Empty;

        // Relacionamentos
        public LoteTransferencia LoteTransferencia { get; set; } = null!;
        public Contacto Contacto { get; set; } = null!;
    }
}
