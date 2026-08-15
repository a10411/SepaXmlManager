using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Models.Entidades;
using SepaXmlManager.Models.Entities; // Ajusta se o teu namespace de entidades for ligeiramente diferente

namespace SepaXmlManager.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Mapeamento das Entidades para Tabelas na Base de Dados
        public DbSet<Company> Empresas { get; set; } = null!;
        public DbSet<Contacts> Contactos { get; set; } = null!;
        public DbSet<TransferBatch> LotesTransferencia { get; set; } = null!;
        public DbSet<Transaction> Transacoes { get; set; } = null!;
    }
}