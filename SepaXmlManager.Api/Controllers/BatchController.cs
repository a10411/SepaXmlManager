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

        // GET
        [HttpGet]
        public async Task<IActionResult> GetAllBatches()
        {
            var batches = await _batchService.GetAllBatchesAsync();
            return Ok(batches);
        }

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

        // POST
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
                // Erro se a Empresa ainda não estiver criada
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }


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
                return NotFound(ex.Message); // O Lote não existe
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message); // O Contacto não existe
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }
    }
}