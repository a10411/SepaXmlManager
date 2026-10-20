namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface ISepaService
    {   
    
        Task<byte[]> GeneratePain001XmlAsync(int batchId);

        Task<byte[]> GeneratePain008XmlAsync(int batchId);



    }
}
