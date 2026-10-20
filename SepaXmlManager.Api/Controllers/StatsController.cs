using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // 1. Total de Contactos registados na app
            var totalContacts = await _context.Contacts.CountAsync();

            // 2. Volume Total de Pagamentos (Saídas de dinheiro)
            var totalPayments = await _context.Transactions
                .Where(t => t.TransferBatch.TypeOperation == "PAGAMENTO")
                .SumAsync(t => t.Amount);

            // 3. Volume Total de Cobranças (Entradas de dinheiro)
            var totalCollections = await _context.Transactions
                .Where(t => t.TransferBatch.TypeOperation == "COBRANCA")
                .SumAsync(t => t.Amount);

            // 4. Total de Lotes (Batches) gerados
            var totalBatches = await _context.TransferBatches.CountAsync();

            // 5. TOP 5 Clientes (Aqueles a quem mais cobramos)
            // Agrupamos as transações de cobrança por Contacto, somamos os valores, ordenamos do maior para o menor e tiramos os primeiros 5.
            var topClients = await _context.Transactions
                .Where(t => t.TransferBatch.TypeOperation == "COBRANCA")
                .GroupBy(t => new { t.Contact.Name, t.Contact.NIF })
                .Select(group => new
                {
                    ClientName = group.Key.Name,
                    TaxId = group.Key.NIF,
                    TotalCharged = group.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.TotalCharged)
                .Take(5)
                .ToListAsync();

            // Montamos a resposta final limpa num único objeto JSON
            var dashboardData = new
            {
                Overview = new
                {
                    TotalContacts = totalContacts,
                    TotalBatchesGenerated = totalBatches,
                    TotalVolumePayments = totalPayments,
                    TotalVolumeCollections = totalCollections,
                    NetBalance = totalCollections - totalPayments // Saldo: O que entrou menos o que saiu
                },
                TopClients = topClients
            };

            return Ok(dashboardData);
        }
    }
}