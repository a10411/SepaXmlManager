using Microsoft.EntityFrameworkCore;
using ProjetoFinal.Models.Pain001;
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Serialization;



namespace SepaXmlManager.Api.Services
{
    public class SepaService : ISepaService
    {
        private readonly AppDbContext _context;

        public SepaService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<byte[]> GeneratePain001XmlAsync(int batchID)
        {   
            //procura o lote
            var batch = await _context.TransferBatches
                .Include(b=> b.Company)
                .Include(b=> b.Transaction)
                    .ThenInclude(t=> t.Contact)
                .FirstOrDefaultAsync(b=> b.Id == batchID);

            //se o lote nao existir bloqueia se o processo 
            if (batch == null)
            {
                throw new KeyNotFoundException($"Transfer Batch with ID {batchID} not found!");
            }

            //mapping para XML

            var doc = new ProjetoFinal.Models.Pain001.Document();

            // CustomerCreditTransferInitiationV09 é o nome oficial para a versao do ficheiro pain.001 atualmente utilizado
            doc.CstmrCdtTrfInitn = new CustomerCreditTransferInitiationV09();


            //preencher o cabeçalho (Group Header)
            doc.CstmrCdtTrfInitn.GrpHdr = new GroupHeader85()
            {
                MsgId = batch.MsgId,
                CreDtTm = batch.DateCreation,
                NbOfTxs = batch.TotalTransactions.ToString(),
                CtrlSum = batch.TotalAmount,
                CtrlSumSpecified = true,
                InitgPty = new PartyIdentification135
                {
                    Nm = batch.Company.Name
                }
            };

            //Preencher a Informação do Pagamento
            var pmtInf = new PaymentInstruction30
            {
                PmtInfId = $"PMT-{batch.MsgId}",
                PmtMtd = PaymentMethod3Code.Trf,
                ReqdExctnDt = new ProjetoFinal.Models.Pain001.DateAndDateTime2Choice
                {
                    Dt = DateTime.Now.Date,
                    DtSpecified = true // O segredo está aqui! Isto força o C# a escrever a tag no XML.
                },

                //Empresa Credora
                Dbtr = new PartyIdentification135 { Nm = batch.Company.Name },
                DbtrAcct = new CashAccount38 //Conta da empresa Credora
                {
                    Id = new AccountIdentification4Choice
                    {
                        Othr = new GenericAccountIdentification1 { Id = batch.Company.IBAN },
                    }
                },
                // Banco da Empresa (Company)
                DbtrAgt = new BranchAndFinancialInstitutionIdentification6
                {
                    FinInstnId = new FinancialInstitutionIdentification18
                    {
                        Bicfi = batch.Company.BIC.ToUpper()
                    }
                }
            };

            // Adicionar Transactions individuais

            //Lista vazia para guardar transações formatadas
            var txList = new List<CreditTransferTransaction34>();

            //Criar a estrutura exigida pelo banco para cada pagamento
            foreach (var tx in batch.Transaction)
            {
                var txInfo = new CreditTransferTransaction34
                {
                    PmtId = new PaymentIdentification6
                    {
                        EndToEndId = tx.EndToEndId,
                    },
                    Amt = new AmountType4Choice
                    {
                        InstdAmt = new ActiveOrHistoricCurrencyAndAmount
                        {
                            Ccy = "EUR",
                            Value = tx.Amount
                        }
                    },
                    CdtrAgt = new BranchAndFinancialInstitutionIdentification6
                    {
                        FinInstnId = new FinancialInstitutionIdentification18
                        {
                            Bicfi = tx.Contact.BIC.ToUpper()
                        }
                    },
                    Cdtr = new PartyIdentification135
                    {
                        Nm = tx.Contact.Name
                    },
                    CdtrAcct = new CashAccount38
                    {
                        Id = new AccountIdentification4Choice
                        {
                            Othr = new GenericAccountIdentification1 { Id = tx.Contact.IBAN}
                        }
                    },
         
                };
                //Adicionar a transacao à lista
                txInfo.RmtInf = new ProjetoFinal.Models.Pain001.RemittanceInformation16();
                txInfo.RmtInf.Ustrd.Add(tx.Description);
                pmtInf.CdtTrfTxInf.Add(txInfo);
            }

            // Colocamos a lista de transações dentro das Informações de Pagamento (PmtInf)
            
            // E colocamos as Informações de Pagamento dentro do Documento principal
            doc.CstmrCdtTrfInitn.PmtInf.Add(pmtInf);

            // Criamos o tradutor que sabe converter a classe 'Document' em XML
            var serializer = new XmlSerializer(typeof(ProjetoFinal.Models.Pain001.Document));

            // Configuramos as regras do ficheiro XML (O banco exige regras estritas)
            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), // O banco exige UTF-8 sem assinatura "BOM"
                Indent = true // Formata o texto com parágrafos e espaços (bonito e legível para humanos)
            };

            // Usamos a Memória RAM (MemoryStream) em vez do disco rígido para criar o ficheiro de forma mais rápida e segura
            using var memoryStream = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(memoryStream, xmlSettings))
            {
                // A magia acontece aqui: o serializer lê o "doc" e escreve o texto XML para dentro da memória
                serializer.Serialize(xmlWriter, doc);
            }

            // Pegamos no ficheiro XML gerado na memória e convertemos para um array de Bytes.
            // É isto que será enviado pela Internet para o utilizador fazer o download!
            return memoryStream.ToArray();
        }
        public async Task<byte[]> GeneratePain008XmlAsync(int batchId)
        {   
            //Procurar lote na base de dados
            var batch = await _context.TransferBatches
                .Include(b => b.Company)
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
            {
                throw new KeyNotFoundException($"Transfer Batch with ID {batchId} not found!");
            }

            //Mapping para XML
            var doc = new ProjetoFinal.Models.Pain008.Document();

            doc.CstmrDrctDbtInitn = new ProjetoFinal.Models.Pain008.CustomerDirectDebitInitiationV08();

            //Preencher o Cabeçalho (Group Header)
            doc.CstmrDrctDbtInitn.GrpHdr = new ProjetoFinal.Models.Pain008.GroupHeader83
            {
                MsgId = batch.MsgId,
                CreDtTm = batch.DateCreation,
                NbOfTxs = batch.TotalTransactions.ToString(),
                CtrlSum = batch.TotalAmount,
                CtrlSumSpecified = true,
                InitgPty = new ProjetoFinal.Models.Pain008.PartyIdentification135
                {
                    Nm = batch.Company.Name
                }
            };


            //Informações do Pagamento (A Empresa agora é quem RECEBE - Credora)
            var pmtInf = new ProjetoFinal.Models.Pain008.PaymentInstruction29
            {
                PmtInfId = $"PMT-{batch.MsgId}",
                PmtMtd = ProjetoFinal.Models.Pain008.PaymentMethod2Code.Dd, // DD = Direct Debit
                ReqdColltnDt = DateTime.Now.Date.AddDays(3),
                NbOfTxs = batch.TotalTransactions.ToString(),
                CtrlSum = batch.TotalAmount,
                CtrlSumSpecified = true,

                // Empresa Credora (A tua Empresa)
                Cdtr = new ProjetoFinal.Models.Pain008.PartyIdentification135 { Nm = batch.Company.Name },
                CdtrAcct = new ProjetoFinal.Models.Pain008.CashAccount38
                {
                    Id = new ProjetoFinal.Models.Pain008.AccountIdentification4Choice
                    {
                        Othr = new ProjetoFinal.Models.Pain008.GenericAccountIdentification1 { Id = batch.Company.IBAN }
                    }
                },
                CdtrAgt = new ProjetoFinal.Models.Pain008.BranchAndFinancialInstitutionIdentification6
                {
                    FinInstnId = new ProjetoFinal.Models.Pain008.FinancialInstitutionIdentification18 { Bicfi = batch.Company.BIC.ToUpper() }
                }
            };

            // Adicionar Transactions individuais

            // Lista temporária em C# para contornar o problema dos Arrays do XSD

            var txList = new List<ProjetoFinal.Models.Pain008.DirectDebitTransactionInformation23>();

            foreach (var tx in batch.Transaction)
            {
                var txInfo = new ProjetoFinal.Models.Pain008.DirectDebitTransactionInformation23
                {
                    PmtId = new ProjetoFinal.Models.Pain008.PaymentIdentification6
                    {
                        EndToEndId = tx.EndToEndId,
                    },
                    InstdAmt = new ProjetoFinal.Models.Pain008.ActiveOrHistoricCurrencyAndAmount
                    {
                        Ccy = "EUR",
                        Value = tx.Amount
                    },

                    // Informação do Mandato (Obrigatório para Cobranças)
                    DrctDbtTx = new ProjetoFinal.Models.Pain008.DirectDebitTransaction10
                    {
                        MndtRltdInf = new ProjetoFinal.Models.Pain008.MandateRelatedInformation14
                        {
                            MndtId = tx.Contact.MandateReference ?? "MND-DESCONHECIDO",
                            DtOfSgntr = tx.Contact.DateSignatureMandate ?? DateTime.Now,
                            DtOfSgntrSpecified = true
                        }
                    },

                    // Contacto Devedor (Quem vai ser cobrado)
                    DbtrAgt = new ProjetoFinal.Models.Pain008.BranchAndFinancialInstitutionIdentification6
                    {
                        FinInstnId = new ProjetoFinal.Models.Pain008.FinancialInstitutionIdentification18
                        {
                            Bicfi = tx.Contact.BIC.ToUpper()
                        }
                    },
                    Dbtr = new ProjetoFinal.Models.Pain008.PartyIdentification135
                    {
                        Nm = tx.Contact.Name
                    },
                    DbtrAcct = new ProjetoFinal.Models.Pain008.CashAccount38
                    {
                        Id = new ProjetoFinal.Models.Pain008.AccountIdentification4Choice
                        {
                            Othr = new ProjetoFinal.Models.Pain008.GenericAccountIdentification1 { Id = tx.Contact.IBAN }
                        }
                    }
                };

                // Adicionar a transação à lista usando o truque do Array na Descrição
                txInfo.RmtInf = new ProjetoFinal.Models.Pain008.RemittanceInformation16();
                txInfo.RmtInf.Ustrd.Add(tx.Description);
                pmtInf.DrctDbtTxInf.Add(txInfo);
            }
    
 
            doc.CstmrDrctDbtInitn.PmtInf.Add(pmtInf);

            var serializer = new XmlSerializer(typeof(ProjetoFinal.Models.Pain008.Document));

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
            };

            // Usamos a Memória RAM (MemoryStream) em vez do disco rígido para criar o ficheiro de forma mais rápida e segura
            using var memoryStream = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(memoryStream, xmlSettings))
            {
                // A magia acontece aqui: o serializer lê o "doc" e escreve o texto XML para dentro da memória
                serializer.Serialize(xmlWriter, doc);
            }

            // Pegamos no ficheiro XML gerado na memória e convertemos para um array de Bytes.
            // É isto que será enviado pela Internet para o utilizador fazer o download!
            return memoryStream.ToArray();

        }



    }
}
