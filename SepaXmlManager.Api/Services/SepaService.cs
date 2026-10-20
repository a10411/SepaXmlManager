using Microsoft.EntityFrameworkCore;
using ProjetoFinal.Models.Pain001; // Usado pelo GeneratePain001XmlAsync
using ProjetoFinal.Models.Pain008; // Usado pelo GeneratePain008XmlAsync
using SepaXmlManager.Api.Data;
using SepaXmlManager.Api.Services.Interfaces;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;

namespace SepaXmlManager.Api.Services
{
    public class SepaService : ISepaService
    {
        private readonly AppDbContext _context;
        private readonly ICompanyService _companyService;
        private readonly IXmlValidationService _xmlValidator;

        public SepaService(AppDbContext context, ICompanyService companyService, IXmlValidationService xmlValidator)
        {
            _context = context;
            _companyService = companyService;
            _xmlValidator = xmlValidator;
        }

        /// <summary>
        /// Task que gera o ficheiro XML SEPA no formato pain.001 a partir de um lote de transferências identificado pelo batchID.
        /// </summary>
        public async Task<byte[]> GeneratePain001XmlAsync(int batchID)
        {
            var batch = await _context.TransferBatches
                .Include(b => b.Company)
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == batchID);

            if (batch == null)
            {
                throw new KeyNotFoundException($"Transfer Batch with ID {batchID} not found!");
            }

            var myCompany = await _companyService.GetCompanyAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Erro: Cannot generate SEPA file. Configure your Company data first.");
            }

            var doc = new ProjetoFinal.Models.Pain001.Document();
            doc.CstmrCdtTrfInitn = new ProjetoFinal.Models.Pain001.CustomerCreditTransferInitiationV09();

            doc.CstmrCdtTrfInitn.GrpHdr = new ProjetoFinal.Models.Pain001.GroupHeader85()
            {
                MsgId = batch.MsgId,
                CreDtTm = batch.DateCreation,
                NbOfTxs = batch.TotalTransactions.ToString(),
                CtrlSum = batch.TotalAmount,
                CtrlSumSpecified = true,
                InitgPty = new ProjetoFinal.Models.Pain001.PartyIdentification135
                {
                    Nm = myCompany.Name
                }
            };

            var pmtInf = new ProjetoFinal.Models.Pain001.PaymentInstruction30
            {
                PmtInfId = $"PMT-{batch.MsgId}",
                PmtMtd = ProjetoFinal.Models.Pain001.PaymentMethod3Code.Trf,
                ReqdExctnDt = new ProjetoFinal.Models.Pain001.DateAndDateTime2Choice
                {
                    Dt = DateTime.Now.Date,
                    DtSpecified = true
                },
                Dbtr = new ProjetoFinal.Models.Pain001.PartyIdentification135 { Nm = myCompany.Name },
                DbtrAcct = new ProjetoFinal.Models.Pain001.CashAccount38
                {
                    Id = new ProjetoFinal.Models.Pain001.AccountIdentification4Choice
                    {
                        Othr = new ProjetoFinal.Models.Pain001.GenericAccountIdentification1 { Id = myCompany.IBAN },
                    }
                },
                DbtrAgt = new ProjetoFinal.Models.Pain001.BranchAndFinancialInstitutionIdentification6
                {
                    FinInstnId = new ProjetoFinal.Models.Pain001.FinancialInstitutionIdentification18
                    {
                        Bicfi = myCompany.BIC.ToUpper()
                    }
                }
            };

            foreach (var tx in batch.Transaction)
            {
                var txInfo = new ProjetoFinal.Models.Pain001.CreditTransferTransaction34
                {
                    PmtId = new ProjetoFinal.Models.Pain001.PaymentIdentification6
                    {
                        EndToEndId = tx.EndToEndId,
                    },
                    Amt = new ProjetoFinal.Models.Pain001.AmountType4Choice
                    {
                        InstdAmt = new ProjetoFinal.Models.Pain001.ActiveOrHistoricCurrencyAndAmount
                        {
                            Ccy = "EUR",
                            Value = tx.Amount
                        }
                    },
                    CdtrAgt = new ProjetoFinal.Models.Pain001.BranchAndFinancialInstitutionIdentification6
                    {
                        FinInstnId = new ProjetoFinal.Models.Pain001.FinancialInstitutionIdentification18
                        {
                            Bicfi = tx.Contact.BIC.ToUpper()
                        }
                    },
                    Cdtr = new ProjetoFinal.Models.Pain001.PartyIdentification135
                    {
                        Nm = tx.Contact.Name
                    },
                    CdtrAcct = new ProjetoFinal.Models.Pain001.CashAccount38
                    {
                        Id = new ProjetoFinal.Models.Pain001.AccountIdentification4Choice
                        {
                            Othr = new ProjetoFinal.Models.Pain001.GenericAccountIdentification1 { Id = tx.Contact.IBAN }
                        }
                    },

                };

                txInfo.RmtInf = new ProjetoFinal.Models.Pain001.RemittanceInformation16();
                txInfo.RmtInf.Ustrd.Add(tx.Description ?? "Transferencia SEPA");
                pmtInf.CdtTrfTxInf.Add(txInfo);
            }

            doc.CstmrCdtTrfInitn.PmtInf.Add(pmtInf);

            var serializer = new XmlSerializer(typeof(ProjetoFinal.Models.Pain001.Document));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09");

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true
            };

            using var memoryStream = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(memoryStream, xmlSettings))
            {
                serializer.Serialize(xmlWriter, doc, namespaces);
            }

            memoryStream.Position = 0;
            var validationResult = _xmlValidator.ValidateXml(memoryStream, "PAIN001");
            if (!validationResult.IsValid)
            {
                string errorMessage = string.Join(" \n", validationResult.Errors);
                throw new InvalidOperationException($"O ficheiro XML gerado é inválido segundo as regras do banco (XSD):\n{errorMessage}");
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Task que gera o ficheiro XML SEPA no formato pain.008 a partir de um lote de transferências identificado pelo batchId.
        /// </summary>
        public async Task<byte[]> GeneratePain008XmlAsync(int batchId)
        {
            var batch = await _context.TransferBatches
                .Include(b => b.Company)
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
            {
                throw new KeyNotFoundException($"Transfer Batch with ID {batchId} not found!");
            }

            var myCompany = await _companyService.GetCompanyAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Erro: Cannot generate SEPA file. Configure your Company data first.");
            }

            var doc = new ProjetoFinal.Models.Pain008.Document();
            doc.CstmrDrctDbtInitn = new ProjetoFinal.Models.Pain008.CustomerDirectDebitInitiationV08();

            doc.CstmrDrctDbtInitn.GrpHdr = new ProjetoFinal.Models.Pain008.GroupHeader83
            {
                MsgId = batch.MsgId,
                CreDtTm = batch.DateCreation,
                NbOfTxs = batch.TotalTransactions.ToString(),
                CtrlSum = batch.TotalAmount,
                CtrlSumSpecified = true,
                InitgPty = new ProjetoFinal.Models.Pain008.PartyIdentification135
                {
                    Nm = myCompany.Name
                }
            };

            var pmtInf = new ProjetoFinal.Models.Pain008.PaymentInstruction29
            {
                PmtInfId = $"PMT-{batch.MsgId}",
                PmtMtd = ProjetoFinal.Models.Pain008.PaymentMethod2Code.Dd,
                ReqdColltnDt = DateTime.Now.Date.AddDays(3),
                NbOfTxs = batch.TotalTransactions.ToString(),
                CtrlSum = batch.TotalAmount,
                CtrlSumSpecified = true,

                Cdtr = new ProjetoFinal.Models.Pain008.PartyIdentification135 { Nm = myCompany.Name },
                CdtrAcct = new ProjetoFinal.Models.Pain008.CashAccount38
                {
                    Id = new ProjetoFinal.Models.Pain008.AccountIdentification4Choice
                    {
                        Othr = new ProjetoFinal.Models.Pain008.GenericAccountIdentification1 { Id = myCompany.IBAN }
                    }
                },
                CdtrAgt = new ProjetoFinal.Models.Pain008.BranchAndFinancialInstitutionIdentification6
                {
                    FinInstnId = new ProjetoFinal.Models.Pain008.FinancialInstitutionIdentification18 { Bicfi = myCompany.BIC.ToUpper() }
                }
            };

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
                    DrctDbtTx = new ProjetoFinal.Models.Pain008.DirectDebitTransaction10
                    {
                        MndtRltdInf = new ProjetoFinal.Models.Pain008.MandateRelatedInformation14
                        {
                            // Adicionamos um fallback: se for Nulo ou Vazio, gera um "MND-DESCONHECIDO" em vez de deixar estalar
                            MndtId = string.IsNullOrWhiteSpace(tx.Contact.MandateReference) ? $"MND-E2E-{tx.Id}" : tx.Contact.MandateReference,
                            DtOfSgntr = tx.Contact.DateSignatureMandate ?? DateTime.Now,
                            DtOfSgntrSpecified = true
                        }
                    },
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

                // ADIÇÃO 1: Tratamento de Nulos na Descrição
                txInfo.RmtInf = new ProjetoFinal.Models.Pain008.RemittanceInformation16();

                // Em vez de tentarmos criar a lista de novo, apenas fazemos o Add na lista que já vem com o objeto.
                txInfo.RmtInf.Ustrd.Add(string.IsNullOrWhiteSpace(tx.Description) ? "Cobrança SEPA" : tx.Description);

                pmtInf.DrctDbtTxInf.Add(txInfo);
            }

            doc.CstmrDrctDbtInitn.PmtInf.Add(pmtInf);

            var serializer = new XmlSerializer(typeof(ProjetoFinal.Models.Pain008.Document));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08");

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
            };

            using var memoryStream = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(memoryStream, xmlSettings))
            {
                serializer.Serialize(xmlWriter, doc, namespaces);
            }

            memoryStream.Position = 0;
            var validationResult = _xmlValidator.ValidateXml(memoryStream, "PAIN008");

            if (!validationResult.IsValid)
            {
                string errorMessage = string.Join(" \n", validationResult.Errors);
                throw new InvalidOperationException($"O ficheiro XML gerado é inválido segundo as regras do banco (XSD):\n{errorMessage}");
            }

            return memoryStream.ToArray();
        }
    }
}