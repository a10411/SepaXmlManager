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
        private readonly IXmlValidationService _validationService;

        // contrutor para injetar o serviço no controller

        public SepaController(ISepaService sepaService, IXmlValidationService validationService)
        {
            _sepaService = sepaService;
            _validationService = validationService; // Inicializa a variável
        }



        //GET 



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

        /// <summary>
        /// Endpoint para gerar o ficheiro XML (pain.008) de Débitos Diretos (Cobranças).
        /// O URL será: GET /api/sepa/download-pain008/1  
        /// </summary>
        [HttpGet("download-pain008/{batchId}")]
        public async Task<IActionResult> DownloadPain008(int batchId)
        {
            try
            {
                // 1. Chama o teu serviço com a lógica limpa que acabaste de escrever
                byte[] xmlBytes = await _sepaService.GeneratePain008XmlAsync(batchId);

                // 2. Dá um nome dinâmico ao ficheiro para não se misturar com os outros
                string fileName = $"SEPA_PAIN008_Lote_{batchId}_{DateTime.Now:yyyyMMddHHmmss}.xml";

                // 3. Devolve o ficheiro XML pronto a descarregar
                return File(xmlBytes, "application/xml", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                // Se o lote não existir (ex: Lote 99)
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Se der algum erro (ex: NullReferenceException por faltar uma lista)
                return BadRequest(new { message = "Erro ao gerar o ficheiro XML SEPA de Débitos Diretos!", details = ex.Message });
            }
        }




        //POST 
        /// <summary>
        /// Endpoint para validar um ficheiro XML contra as regras oficiais SEPA (XSD).
        /// </summary>
        [HttpPost("validate/{documentType}")]
        public IActionResult ValidateXmlFile(string documentType, IFormFile xmlFile)
        {
            if (xmlFile == null || xmlFile.Length == 0)
            {
                return BadRequest(new { message = "Por favor, envie um ficheiro XML válido." });
            }

            // Passamos o ficheiro diretamente para a memória do nosso Validador
            using (var stream = xmlFile.OpenReadStream())
            {
                var validationResult = _validationService.ValidateXml(stream, documentType);

                // Se quiseres ser extra rigoroso: se der falso, podes devolver um BadRequest
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult);
                }

                // Se passou na validação, devolve Ok!
                return Ok(validationResult);
            }
        }










    }
}