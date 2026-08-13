using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entidades
{
    public class Contacto
    {
        [Key]
        public int Id { get; set; }

        public int EmpresaId { get; set; } // Foreign Key

        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(34)]
        public string IBAN { get; set; } = string.Empty;

        [MaxLength(11)]
        public string BIC { get; set; } = string.Empty;

        [MaxLength(35)]
        public string ReferenciaMandato { get; set; } = string.Empty;// Para pain.008

        public DateTime? DataAssinaturaMandato { get; set; } // Para pain.008

        // Relacionamentos
        public Empresa Empresa { get; set; } = null!;
        public List<Transacao> Transacoes { get; set; } = new List<Transacao>();
    }
}
