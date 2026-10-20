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

        /// <summary>
        /// Obtém os indicadores de desempenho (KPIs) agregados para a construção do painel de controlo principal (dashboard).
        /// </summary>
        /// <returns>Retorna um objeto estruturado contendo o resumo financeiro (pagamentos, cobranças e saldo líquido) e a listagem dos 5 principais clientes (HTTP 200).</returns>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Calcula o número total de contactos registados na base de dados.
            var totalContacts = await _context.Contacts.CountAsync();

            // Calcula o volume total processado em operações de pagamento (saídas de capital).
            var totalPayments = await _context.Transactions
                .Where(t => t.TransferBatch.TypeOperation == "PAGAMENTO")
                .SumAsync(t => t.Amount);

            // Calcula o volume total processado em operações de cobrança (entradas de capital).
            var totalCollections = await _context.Transactions
                .Where(t => t.TransferBatch.TypeOperation == "COBRANCA")
                .SumAsync(t => t.Amount);

            // Contabiliza o número total de lotes de transferências gerados no sistema.
            var totalBatches = await _context.TransferBatches.CountAsync();

            // Agrupa as transações de cobrança por contacto, calcula o montante acumulado e seleciona os cinco clientes com maior volume de faturação.
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

            // Constrói o objeto de resposta estruturado agregando todas as métricas calculadas.
            var dashboardData = new
            {
                Overview = new
                {
                    TotalContacts = totalContacts,
                    TotalBatchesGenerated = totalBatches,
                    TotalVolumePayments = totalPayments,
                    TotalVolumeCollections = totalCollections,
                    NetBalance = totalCollections - totalPayments // Saldo líquido financeiro: diferença entre o volume de cobranças e o volume de pagamentos.
                },
                TopClients = topClients
            };

            return Ok(dashboardData);
        }
    }
}