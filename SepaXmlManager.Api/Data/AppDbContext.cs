using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Models.Entities;

namespace SepaXmlManager.Api.Data
{
    /// <summary>
    /// Representa o contexto principal da base de dados, agregando as entidades de domínio e a infraestrutura de segurança (Identity) para autenticação e autorização.
    /// </summary>
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        /// <summary>
        /// Inicializa uma nova instância do contexto de base de dados com as opções de configuração especificadas.
        /// </summary>
        /// <param name="options">As configurações de injeção de dependência e provider relacional para o contexto.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Mapeamento das Entidades para Tabelas na Base de Dados
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<TransferBatch> TransferBatches { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        /// <summary>
        /// Configura o modelo de dados, as propriedades intrínsecas e as restrições de integridade referencial (relações e chaves estrangeiras).
        /// </summary>
        /// <param name="modelBuilder">A interface responsável pela configuração do modelo relacional no Entity Framework.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // A invocação do método base é estritamente obrigatória quando se herda de IdentityDbContext,
            // garantindo a construção automática das tabelas de gestão de acessos (Identity).
            base.OnModelCreating(modelBuilder);

            // Define a precisão e escala das propriedades de cariz monetário para assegurar a conformidade da integridade financeira no SQL.
            modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<TransferBatch>().Property(t => t.TotalAmount).HasColumnType("decimal(18,2)");

            // Configura o comportamento relacional entre a Transação e o Contacto.
            // Aplica a restrição de eliminação (DeleteBehavior.Restrict) de forma a prevenir violações de integridade e mitigar a eliminação em cascata (Cascade Delete), garantindo o rasto de auditoria.
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Contact)
                .WithMany(c => c.Transaction)
                .HasForeignKey(t => t.ContactId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}