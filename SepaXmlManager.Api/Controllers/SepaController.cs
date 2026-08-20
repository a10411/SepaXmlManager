using Microsoft.AspNetCore.Mvc;
using SepaXmlManager.Api.Services;

namespace SepaXmlManager.Api.Controllers
{
    
    [ApiController]

    //  define o URL
    [Route("api/[controller]")]
    public class SepaController : ControllerBase
    {
        // Variável para guardar o serviço
        private readonly ISepaService _sepaService;

        // contrutor para injetar o serviço no controller
        public SepaController(ISepaService sepaService)
        {
            _sepaService = sepaService;
        }

        /// <summary>
        /// Endpoint para gerar o ficheiro XML (pain.001) e devolvê-lo para download.
        /// O URL será: GET /api/sepa/download-pain001/1  
        /// </summary>
        [HttpGet("download-pain001/{batchId}")]
        public async Task<IActionResult> DownloadPain001(int batchId)
        {
            try
            {
                // 1. Mandamos o Serviço trabalhar e gerar o ficheiro em Memória (Array de Bytes)
                byte[] xmlBytes = await _sepaService.GeneratePain001XmlAsync(batchId);

                // 2. Damos um nome dinâmico ao ficheiro para o cliente descarregar
                // Exemplo: SEPA_PAIN001_Lote_1_20260817.xml
                string fileName = $"SEPA_PAIN001_Lote_{batchId}_{DateTime.Now:yyyyMMddHHmmss}.xml";

                // 3. Devolvemos o ficheiro ao utilizador com o formato "application/xml"
                return File(xmlBytes, "application/xml", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                // Se o Lote não existir na BD, devolvemos um erro 404 (Not Found)
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Se rebentar por outro motivo, devolvemos erro 400 (Bad Request)
                return BadRequest(new { message = "Error generating XML SEPA file!", details = ex.Message });
            }
        }
    }
}