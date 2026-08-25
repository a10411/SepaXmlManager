using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities; // Ajusta se a tua classe Contact estiver noutro namespace (ex: ProjetoFinal.Models)
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace SepaXmlManager.Api.Services
{
    public class ContactService : IContactService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessValidationService _validationService;

        //Injecta o contexto da base de dados e o serviço de validação
        public ContactService(AppDbContext context, IBusinessValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        public async Task<IEnumerable<Contact>> GetAllContactsAsync()
        {
            return await _context.Contacts.ToListAsync();
        }

        public async Task<Contact> GetContactByIdAsync(int id)
        {
            return await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Contact> CreateContactAsync(Contact contact)
        {
            if (!_validationService.IsIbanValid(contact.IBAN))
            {
                throw new ArgumentException("IBAN not valid! Verify data.");
            }

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            return contact;
        }

        public async Task<Contact> UpdateContactAsync(int id, Contact contact)
        {
            var existingContact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);

            if (existingContact == null)
            {
                return null; // Devolvemos nulo para o Controlador saber e atirar um "404 Not Found"
            }

            // Validamos o novo IBAN antes de atualizar
            if (!_validationService.IsIbanValid(contact.IBAN))
            {
                throw new ArgumentException("O novo IBAN do contacto é inválido. Verifique os dados!");
            }

            // Atualizamos a informação
            existingContact.Name = contact.Name;
            existingContact.NIF = contact.NIF;
            existingContact.IBAN = contact.IBAN;
            existingContact.BIC = contact.BIC;
            existingContact.IsActive = contact.IsActive;
            existingContact.MandateReference = contact.MandateReference;
            existingContact.DateSignatureMandate = contact.DateSignatureMandate;

            await _context.SaveChangesAsync();

            return existingContact;
        }

        public async Task<bool> DeleteContactAsync(int id)
        {
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return false;
            }

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
