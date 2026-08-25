using SepaXmlManager.Models.Entities;

namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface IContactService
    {
        Task<IEnumerable<Contact>> GetAllContactsAsync();
        Task<Contact> GetContactByIdAsync(int id);
        Task<Contact> CreateContactAsync(Contact contact);
        Task<Contact> UpdateContactAsync(int id, Contact contact);
        Task<bool> DeleteContactAsync(int id);
    }
}
