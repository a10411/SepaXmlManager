using Microsoft.AspNetCore.Mvc;
using SepaXmlManager.Api.Services;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchController : ControllerBase
    {
        private readonly IBatchService _batchService;

        public BatchController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        /// <summary>
        /// Obtém a lista de todos os lotes de transferências ou cobranças registados no sistema.
        /// </summary>
        /// <returns>Retorna um objeto IActionResult contendo a lista de lotes e o código de estado HTTP 200 (OK).</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllBatches()
        {
            var batches = await _batchService.GetAllBatchesAsync();
            return Ok(batches);
        }

        /// <summary>
        /// Obtém os detalhes de um lote específico através do seu identificador.
        /// </summary>
        /// <param name="id">O identificador numérico único do lote a recuperar.</param>
        /// <returns>Retorna os dados do lote solicitado (HTTP 200) ou uma indicação de que não foi encontrado (HTTP 404).</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBatchById(int id)
        {
            var batch = await _batchService.GetBatchByIdAsync(id);
            if (batch == null)
            {
                return NotFound($"Batch with id {id} not found!");
            }
            return Ok(batch);
        }

        /// <summary>
        /// Cria um novo lote de transferências ou cobranças no sistema.
        /// </summary>
        /// <param name="batch">O objeto contendo os dados base do lote a registar.</param>
        /// <returns>Retorna o lote criado acompanhado do URI de acesso (HTTP 201), ou uma mensagem de erro caso a empresa principal não esteja configurada (HTTP 400).</returns>
        [HttpPost]
        public async Task<IActionResult> CreateBatch([FromBody] TransferBatch batch)
        {
            if (batch == null)
            {
                return BadRequest("Batch data cannot be null.");
            }

            try
            {
                var createdBatch = await _batchService.CreateBatchAsync(batch);
                return CreatedAtAction(nameof(GetBatchById), new { id = createdBatch.Id }, createdBatch);
            }
            catch (InvalidOperationException ex)
            {
                // Retorna erro caso a entidade Empresa principal não se encontre previamente configurada no sistema.
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Associa uma nova transação financeira a um lote pré-existente.
        /// </summary>
        /// <param name="batchId">O identificador numérico do lote de destino.</param>
        /// <param name="transaction">O objeto com os dados da transação a ser adicionada.</param>
        /// <returns>Retorna a transação registada (HTTP 200), um aviso de lote não encontrado (HTTP 404), ou um erro de validação caso o contacto não exista (HTTP 400).</returns>
        [HttpPost("{batchId}/Transaction")]
        public async Task<IActionResult> AddTransactionToBatch(int batchId, [FromBody] Transaction transaction)
        {
            if (transaction == null)
            {
                return BadRequest("Transaction data cannot be null.");
            }

            try
            {
                var createdTransaction = await _batchService.AddTransactionToBatchAsync(batchId, transaction);
                return Ok(createdTransaction);
            }
            catch (KeyNotFoundException ex)
            {
                // Sinaliza a ausência de recurso (Http 404) caso o identificador do lote referenciado não exista.
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Assinala um erro de validação (Http 400) caso a transação tente associar um contacto inexistente.
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }
    }
}