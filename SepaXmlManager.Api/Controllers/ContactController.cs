using Microsoft.AspNetCore.Mvc;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities;
using System;
using System.Threading.Tasks;

namespace SepaXmlManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// Obtém a lista de todos os contactos registados no sistema.
        /// </summary>
        /// <returns>Retorna um objeto IActionResult contendo a lista integral de contactos e o código de estado HTTP 200 (OK).</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllContacts()
        {
            var contacts = await _contactService.GetAllContactsAsync();
            return Ok(contacts);
        }

        /// <summary>
        /// Obtém os detalhes de um contacto específico através do seu identificador.
        /// </summary>
        /// <param name="id">O identificador numérico único do contacto a recuperar.</param>
        /// <returns>Retorna os dados do contacto solicitado (HTTP 200) ou uma indicação de que não foi encontrado (HTTP 404).</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactById(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);

            if (contact == null)
            {
                return NotFound($"Contact with ID {id} not found.");
            }

            return Ok(contact);
        }

        /// <summary>
        /// Regista um novo contacto. Requer a validação de regras de negócio estritas (ex: conformidade do IBAN e NIF).
        /// </summary>
        /// <param name="contact">O objeto contendo os dados do contacto a submeter.</param>
        /// <returns>Retorna o contacto criado acompanhado do URI de acesso (HTTP 201), ou uma mensagem de erro em caso de falha de validação (HTTP 400).</returns>
        [HttpPost]
        public async Task<IActionResult> CreateContact([FromBody] Contact contact)
        {
            if (contact == null)
            {
                return BadRequest("Contact data cannot be null.");
            }

            try
            {
                var createdContact = await _contactService.CreateContactAsync(contact);

                // Retorna o status 201 Created em conformidade com as normas REST para criação de recursos.
                return CreatedAtAction(nameof(GetContactById), new { id = createdContact.Id }, createdContact);
            }
            catch (ArgumentException ex)
            {
                // Interceta exceções de negócio decorrentes de dados inválidos submetidos pelo cliente.
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// Atualiza as informações de um contacto pré-existente.
        /// </summary>
        /// <param name="id">O identificador numérico único do contacto a ser atualizado.</param>
        /// <param name="contact">O objeto com a informação atualizada do contacto.</param>
        /// <returns>Retorna os dados atualizados (HTTP 200) ou uma mensagem de erro de validação (HTTP 400).</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] Contact contact)
        {
            if (contact == null)
            {
                return BadRequest("Contact data cannot be null.");
            }

            try
            {
                var updatedContact = await _contactService.UpdateContactAsync(id, contact);

                if (updatedContact == null)
                {
                    return NotFound($"Contact with ID {id} not found.");
                }

                return Ok(updatedContact);
            }
            catch (ArgumentException ex)
            {
                // Garante que a validação de regras de negócio também é aplicada na atualização dos dados.
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove um contacto do sistema com base no identificador fornecido.
        /// </summary>
        /// <param name="id">O identificador numérico único do contacto a eliminar.</param>
        /// <returns>Retorna um estado sem conteúdo (HTTP 204) após o sucesso da operação, ou um aviso de recurso não encontrado (HTTP 404).</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var success = await _contactService.DeleteContactAsync(id);

            if (!success)
            {
                return NotFound($"Contact with ID {id} not found.");
            }

            // Retorna o status 204 No Content indicando a eliminação bem-sucedida do recurso sem dados adicionais para apresentar.
            return NoContent();
        }
    }
}