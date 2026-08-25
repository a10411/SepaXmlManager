namespace SepaXmlManager.Api.Services.Interfaces
{
    public interface ISepaService
    {   
        /// <summary>
        /// Gera ficheiro XML no formato ISO 20022(pain.001) para um lote de transferências.
        /// </summary>
        /// <param name="batchId"></param>
        /// <returns></returns>
        Task<byte[]> GeneratePain001XmlAsync(int batchId);

        /// <summary>
        /// Gera ficheiro XML no formato ISO 20022(pain.008) para um lote de débitos diretos.
        /// </summary>
        /// <param name="batchId"></param>
        /// <returns></returns>
        Task<byte[]> GeneratePain008XmlAsync(int batchId);



    }
}
