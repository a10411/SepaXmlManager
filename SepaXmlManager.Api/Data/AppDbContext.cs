using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Models.Entities; // Ajusta se o teu namespace de entidades for ligeiramente diferente

namespace SepaXmlManager.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Mapeamento das Entidades para Tabelas na Base de Dados
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<TransferBatch> TransferBatches { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Resolve o aviso do tamanho do dinheiro no SQL
            modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransferBatch>().Property(t => t.TotalAmount).HasColumnType("decimal(18,2)");

            // 2. Resolve o Erro Fatal do "Efeito Dominó" (Cascade Delete)
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Contact) // ATENÇÃO: Confirma se na tua classe Transaction a propriedade se chama 'Contact' ou 'Contacto'
                .WithMany(c => c.Transaction)
                .HasForeignKey(t => t.ContactId)
                .OnDelete(DeleteBehavior.Restrict); // É esta palavra mágica que resolve o teu problema!
        }
    }


}