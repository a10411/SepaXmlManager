using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities; 
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
                throw new ArgumentException("O IBAN do contacto é inválido. Verifique os dados!");
            }

            // 2. A MAGIA: Ligar o cliente automaticamente à única Empresa do sistema
            var myCompany = await _context.Companies.FirstOrDefaultAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Erro: Tem de configurar a sua Empresa no sistema antes de criar clientes.");
            }

            // Atribuímos o ID da empresa verdadeira ao contacto antes de o gravar!
            contact.CompanyId = myCompany.Id;
            if (!_validationService.IsValidTaxId("PT", contact.NIF)) // Podes assumir "PT" por defeito se não tiveres o campo Country
            {
                throw new ArgumentException("O NIF introduzido é inválido ou não obedece às regras matemáticas europeias.");
            }
            // 3. Guarda na base de dados em segurança
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync(); // Era aqui que o erro 500 rebentava!

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
