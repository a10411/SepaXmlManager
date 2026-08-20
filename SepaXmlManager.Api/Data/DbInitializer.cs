
using SepaXmlManager.Models.Entities;

namespace SepaXmlManager.Api.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            // Garantir que a Base de Dados está criada
            //context.Database.EnsureCreated();

            // Se já existirem empresas na BD, não faz nada (já tem dados)
            if (context.Companies.Any())
            {
                return;
            }

            // 1. Criar uma Empresa de Teste
            var company = new Company
            {
                Name = "Empresa Exemplo Lda",
                NIF = "501234567",
                IBAN = "PT50000700000001234567822",
                BIC = "BESRPTPLxxx",
                IsActive = true
            };
            context.Companies.Add(company);
            context.SaveChanges(); // Guarda para gerar o ID da Empresa

            // 2. Criar Contactos (Beneficiários) de Teste
            var contact1 = new Contact
            {
                CompanyId = company.Id,
                Name = "João Silva",
                NIF = "212345678",
                IBAN = "PT50003300000009876543211",
                BIC = "BCOMPTPLxxx",
                IsActive = true
            };

            var contact2 = new Contact
            {
                CompanyId = company.Id,
                Name = "Maria Santos",
                NIF = "223456789",
                IBAN = "PT50001800000005554443322",
                BIC = "CGDPTPLXxxx",
                IsActive = true
            };

            context.Contacts.AddRange(contact1, contact2);
            context.SaveChanges();

            // 3. Criar um Lote de Transferências (TransferBatch) de Teste
            var batch = new TransferBatch
            {
                CompanyId = company.Id,
                MsgId = $"MSG-{DateTime.Now:yyyyMMddHHmmss}",
                DateCreation = DateTime.Now,
                TotalTransactions = 2,
                TotalAmount = 250.50m,
                TypeOperation = "PAIN001"
            };
            context.TransferBatches.Add(batch);
            context.SaveChanges();

            // 4. Criar as Transações do Lote
            var tx1 = new Transaction
            {
                TransferBatchId = batch.Id,
                ContactId = contact1.Id,
                EndToEndId = $"E2E-{batch.Id}-001",
                Amount = 100.00m,
                Description = "Pagamento Fatura 101"
            };

            var tx2 = new Transaction
            {
                TransferBatchId = batch.Id,
                ContactId = contact2.Id,
                EndToEndId = $"E2E-{batch.Id}-002",
                Amount = 150.50m,
                Description = "Pagamento Fatura 102"
            };

            context.Transactions.AddRange(tx1, tx2);
            context.SaveChanges();
        }
    }
}