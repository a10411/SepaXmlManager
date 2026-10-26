using Microsoft.EntityFrameworkCore;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using SepaXmlManager.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SepaXmlManager.Api.Services
{
    public class ContactService : IContactService
    {
        private readonly AppDbContext _context;
        private readonly IBusinessValidationService _validationService;

        // Injeta as dependências do contexto da base de dados e do serviço de validação de negócio.
        public ContactService(AppDbContext context, IBusinessValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        /// <summary>
        /// Obtém a listagem completa de todos os contactos registados na base de dados.
        /// </summary>
        /// <returns>Retorna uma coleção contendo todas as entidades de contactos.</returns>
        public async Task<IEnumerable<Contact>> GetAllContactsAsync()
        {
            return await _context.Contacts.Where(c => c.IsActive).ToListAsync();
        }

        /// <summary>
        /// Obtém os detalhes de um contacto específico através do seu identificador único.
        /// </summary>
        /// <param name="id">O identificador numérico do contacto a recuperar.</param>
        /// <returns>Retorna a entidade do contacto correspondente, ou nulo caso o registo não seja encontrado.</returns>
        public async Task<Contact> GetContactByIdAsync(int id)
        {
            return await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Regista um novo contacto no sistema, garantindo a associação à entidade emissora principal e a validação das regras de negócio aplicáveis (IBAN e NIF).
        /// </summary>
        /// <param name="contact">A entidade contendo os dados do novo contacto a criar.</param>
        /// <returns>Retorna o contacto recém-criado após a persistência bem-sucedida na base de dados.</returns>
        /// <exception cref="ArgumentException">Lançada caso os dados fornecidos, como o IBAN ou NIF, falhem as validações estruturais e matemáticas (ex: Módulo 97).</exception>
        /// <exception cref="InvalidOperationException">Lançada caso a entidade da Empresa não se encontre configurada no sistema antes da criação do contacto.</exception>
        public async Task<Contact> CreateContactAsync(Contact contact)
        {
            // Valida o formato e integridade do IBAN submetido.
            if (!_validationService.IsIbanValid(contact.IBAN))
            {
                throw new ArgumentException("O IBAN do contacto é inválido. Verifique os dados!");
            }

            // Associa o cliente automaticamente à entidade Empresa configurada no sistema.
            var myCompany = await _context.Companies.FirstOrDefaultAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Erro: Tem de configurar a sua Empresa no sistema antes de criar clientes.");
            }

            // Atribui o identificador da empresa emissora ao contacto antes da persistência dos dados.
            contact.CompanyId = myCompany.Id;

            // Efetua a validação do número de identificação fiscal (assumindo prefixo "PT" por omissão na validação).
            if (!_validationService.IsValidTaxId("PT", contact.NIF))
            {
                throw new ArgumentException("O NIF introduzido é inválido ou não obedece às regras matemáticas europeias.");
            }

            // Insere o registo e consolida as alterações na base de dados.
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            return contact;
        }

        /// <summary>
        /// Atualiza as informações financeiras e de identificação de um contacto pré-existente.
        /// </summary>
        /// <param name="id">O identificador numérico do contacto a modificar.</param>
        /// <param name="contact">O objeto com os novos dados a serem aplicados.</param>
        /// <returns>Retorna a entidade atualizada ou nulo caso não exista registo prévio correspondente ao identificador fornecido.</returns>
        /// <exception cref="ArgumentException">Lançada caso o novo IBAN fornecido seja inválido estrutural ou matematicamente.</exception>
        public async Task<Contact> UpdateContactAsync(int id, Contact contact)
        {
            var existingContact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);

            if (existingContact == null)
            {
                // Retorna nulo de forma a sinalizar ao controlador a ausência do registo (possibilitando a emissão de um 404 Not Found).
                return null;
            }

            // Executa a validação de regras de negócio para o novo IBAN antes de proceder à atualização.
            if (!_validationService.IsIbanValid(contact.IBAN))
            {
                throw new ArgumentException("O novo IBAN do contacto é inválido. Verifique os dados!");
            }

            // Efetua a atualização dos dados da entidade.
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

        /// <summary>
        /// Remove definitivamente um contacto da base de dados.
        /// </summary>
        /// <param name="id">O identificador numérico único do contacto a eliminar.</param>
        /// <returns>Retorna verdadeiro (true) se a operação for concluída com sucesso, ou falso (false) caso o contacto não exista.</returns>
        public async Task<bool> DeleteContactAsync(int id)
        {
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return false;
            }

            contact.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}