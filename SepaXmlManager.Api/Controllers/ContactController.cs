using Microsoft.AspNetCore.Mvc;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities; // Ajusta se a tua classe Contact estiver noutro namespace
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
        /// Endpoint para obter todos os contactos.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllContacts()
        {
            var contacts = await _contactService.GetAllContactsAsync();
            return Ok(contacts);
        }

        /// <summary>
        /// Endpoint para obter um contacto específico pelo seu ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
        /// Endpoint para criar um novo contacto. Se o IBAN for inválido, retorna um erro 400 Bad Request.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
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
                // Devolvemos um código 201 Created (Padrão de Ouro em APIs REST)
                return CreatedAtAction(nameof(GetContactById), new { id = createdContact.Id }, createdContact);
            }
            catch (ArgumentException ex)
            {
                // Apanha o erro do IBAN falso!
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// Endpoint para atualizar um contacto existente. Se o IBAN for inválido, retorna um erro 400 Bad Request.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="contact"></param>
        /// <returns></returns>
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
                // Apanha o erro se o utilizador tentar editar o cliente e meter um IBAN falso
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// Endpoint para apagar um contacto pelo seu ID. Se o contacto não existir, retorna um erro 404 Not Found.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var success = await _contactService.DeleteContactAsync(id);

            if (!success)
            {
                return NotFound($"Contact with ID {id} not found.");
            }

            return NoContent(); // Código 204: Significa "Tudo correu bem e já não há nada para mostrar"
        }
    }
}