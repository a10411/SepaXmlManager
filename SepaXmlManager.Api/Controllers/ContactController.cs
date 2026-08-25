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


        [HttpGet]
        public async Task<IActionResult> GetAllContacts()
        {
            var contacts = await _contactService.GetAllContactsAsync();
            return Ok(contacts);
        }

        // GET: api/Contact/5 (Procura apenas o contacto com o ID número 5)
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

        // POST: api/Contact (Cria um contacto novo)
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

        // PUT: api/Contact/5 (Atualiza os dados do contacto com o ID número 5)
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

        // DELETE: api/Contact/5 (Apaga o contacto com o ID número 5)
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