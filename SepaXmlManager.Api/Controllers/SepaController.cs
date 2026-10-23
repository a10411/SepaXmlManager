using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SepaXmlManager.Api.Services;
using SepaXmlManager.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SepaController : ControllerBase
    {
        private readonly ISepaService _sepaService;
        private readonly IXmlValidationService _validationService;
        private readonly IUploadService _uploadService;

        // Injeta as dependências dos serviços necessários para a orquestração e geração de ficheiros SEPA.
        public SepaController(ISepaService sepaService, IXmlValidationService validationService, IUploadService uploadService)
        {
            _sepaService = sepaService;
            _validationService = validationService;
            _uploadService = uploadService;
        }

        /// <summary>
        /// Gera o ficheiro XML de transferências a crédito (pain.001) e disponibiliza-o para download.
        /// </summary>
        /// <param name="batchId">O identificador numérico único do lote de transferências.</param>
        /// <returns>Retorna o ficheiro XML gerado em formato binário (HTTP 200), ou as respetivas mensagens de erro caso o lote não exista (HTTP 404) ou ocorra uma anomalia (HTTP 400).</returns>
        [HttpGet("download-pain001/{batchId}")]
        [Authorize(Roles = "Tesouraria")]
        public async Task<IActionResult> DownloadPain001(int batchId)
        {
            try
            {
                // Invoca o serviço para gerar a estrutura estruturada do ficheiro XML em memória.
                byte[] xmlBytes = await _sepaService.GeneratePain001XmlAsync(batchId);

                // Define um nome dinâmico para o ficheiro de saída baseado no identificador do lote e na data de processamento.
                string fileName = $"SEPA_PAIN001_Lote_{batchId}_{DateTime.Now:yyyyMMddHHmmss}.xml";

                // Retorna o conteúdo em formato de ficheiro transferível (application/xml).
                return File(xmlBytes, "application/xml", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                // Retorna o status 404 (Not Found) caso o lote referenciado não seja encontrado na base de dados.
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Interceta falhas de geração ou formatação, retornando o status 400 (Bad Request).
                return BadRequest(new { message = "Error generating XML SEPA file!", details = ex.Message });
            }
        }

        /// <summary>
        /// Gera o ficheiro XML de débitos diretos/cobranças (pain.008) e disponibiliza-o para download.
        /// </summary>
        /// <param name="batchId">O identificador numérico único do lote de cobranças.</param>
        /// <returns>Retorna o ficheiro XML gerado (HTTP 200) ou a correspondente mensagem de falha em caso de anomalia estrutural ou de negócio.</returns>
        [HttpGet("download-pain008/{batchId}")]
        [Authorize(Roles = "Tesouraria")]
        public async Task<IActionResult> DownloadPain008(int batchId)
        {
            try
            {
                // Processa a geração do ficheiro de cobranças baseado no standard ISO.
                byte[] xmlBytes = await _sepaService.GeneratePain008XmlAsync(batchId);

                // Atribui a nomenclatura padronizada ao documento.
                string fileName = $"SEPA_PAIN008_Lote_{batchId}_{DateTime.Now:yyyyMMddHHmmss}.xml";

                // Disponibiliza o ficheiro gerado para download direto pelo utilizador.
                return File(xmlBytes, "application/xml", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                // Assinala a inexistência do lote na base de dados (HTTP 404).
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Retorna informação detalhada sobre erros durante o processamento do ficheiro XML (HTTP 400).
                return BadRequest(new { message = "Erro ao gerar o ficheiro XML SEPA de Débitos Diretos!", details = ex.Message });
            }
        }

        /// <summary>
        /// Efetua a validação estrutural de um ficheiro XML contra as regras oficiais SEPA (ficheiro XSD do banco).
        /// </summary>
        /// <param name="documentType">A designação do standard utilizado (ex: PAIN001 ou PAIN008).</param>
        /// <param name="xmlFile">O ficheiro em formato XML submetido para análise.</param>
        /// <returns>Retorna o sumário da validação técnica (HTTP 200) ou o detalhe de anomalias encontradas caso as regras XSD não sejam cumpridas (HTTP 400).</returns>
        [HttpPost("validate/{documentType}")]
        [Authorize(Roles = "Tesouraria")]
        public IActionResult ValidateXmlFile(string documentType, IFormFile xmlFile)
        {
            if (xmlFile == null || xmlFile.Length == 0)
            {
                return BadRequest(new { message = "Por favor, envie um ficheiro XML válido." });
            }

            // Transfere o fluxo de dados diretamente para o serviço de validação.
            using (var stream = xmlFile.OpenReadStream())
            {
                var validationResult = _validationService.ValidateXml(stream, documentType);

                // Retorna o status 400 (Bad Request) em caso de falha estrutural, impedindo o processamento de um ficheiro inválido.
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult);
                }

                // Confirma que o ficheiro cumpre rigorosamente os parâmetros de conformidade técnica do standard definido.
                return Ok(validationResult);
            }
        }

        /// <summary>
        /// Processa a importação de um ficheiro CSV contendo transações financeiras, instanciando automaticamente novos contactos e compilando um lote de processamento.
        /// </summary>
        /// <param name="file">O ficheiro de origem formatado em CSV.</param>
        /// <param name="operationType">Define o tipo de operação financeira a ser processada ("PAGAMENTO" ou "COBRANCA"). O valor padrão é "PAGAMENTO".</param>
        /// <returns>Retorna o identificador numérico único do lote criado (HTTP 200) ou a identificação das incorreções detetadas nos dados fornecidos (HTTP 400).</returns>
        [HttpPost("upload-batch")]
        [Authorize(Roles = "Tesouraria")]
        public async Task<IActionResult> UploadCsvBatch(IFormFile file, [FromQuery] string operationType = "PAGAMENTO")
        {
            try
            {
                // Inicia o processo de leitura, mapeamento e criação das entidades com base nos registos do ficheiro CSV.
                int batchId = await _uploadService.ProcessCsvFileAsync(file, operationType);

                // Retorna a confirmação da operação bem-sucedida em conjunto com o identificador do lote recém-criado.
                return Ok(new { message = "Ficheiro processado com sucesso!", batchId = batchId });
            }
            catch (ArgumentException ex)
            {
                // Interceta problemas de formatação intrínsecos aos dados do ficheiro (ex: estrutura incorreta, campos de preenchimento obrigatório omissos ou IBAN inválido).
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Retorna um erro de negócio caso a entidade Empresa principal não se encontre adequadamente configurada no sistema antes da importação.
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Erro interno do servidor: {ex.Message}" });
            }
        }
    }
}