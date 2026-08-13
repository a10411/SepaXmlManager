using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SepaXmlManager.Models.Entidades
{
    public class LoteTransferencia
    {
        [Key]
        public int Id { get; set; }

        public int EmpresaId { get; set; } // Foreign Key

        [Required, MaxLength(35)]
        public string MsgId { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; }

        [Required, MaxLength(10)]
        public string TipoOperacao { get; set; } = string.Empty;// "PAIN.001" ou "PAIN.008"

        public int TotalTransacoes { get; set; }

        public decimal MontanteTotal { get; set; }

        public string CaminhoFicheiro { get; set; } = string.Empty;

        // Relacionamentos
        public Empresa Empresa { get; set; } = null!;
        public List<Transacao> Transacoes { get; set; } = new List<Transacao>();
    }
}
