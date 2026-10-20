using Microsoft.EntityFrameworkCore;
using ProjetoFinal.Models.Pain001;
using ProjetoFinal.Models.Pain008;
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
        /// Gera o ficheiro XML SEPA no formato pain.001 (Transferências a Crédito) a partir de um lote identificado.
        /// </summary>
        /// <param name="batchId">O identificador numérico único do lote de transferências a processar.</param>
        /// <returns>Retorna um array de bytes contendo o documento XML estruturado e validado, pronto para exportação.</returns>
        /// <exception cref="KeyNotFoundException">Lançada caso o lote especificado não seja encontrado na base de dados.</exception>
        /// <exception cref="InvalidOperationException">Lançada caso a entidade emissora não esteja configurada ou o ficheiro gerado viole as regras de validação XSD.</exception>
        public async Task<byte[]> GeneratePain001XmlAsync(int batchId)
        {
            // Obtém o lote de transferências da base de dados, incluindo as respetivas relações hierárquicas.
            var batch = await _context.TransferBatches
                .Include(b => b.Company)
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
            {
                throw new KeyNotFoundException($"Transfer Batch with ID {batchId} not found!");
            }

            // Obtém os dados da empresa emissora necessários para preencher o cabeçalho.
            var myCompany = await _companyService.GetCompanyAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Erro: Cannot generate SEPA file. Configure your Company data first.");
            }

            // Inicializa a estrutura base do documento XML segundo o standard pain.001.
            var doc = new ProjetoFinal.Models.Pain001.Document();
            doc.CstmrCdtTrfInitn = new ProjetoFinal.Models.Pain001.CustomerCreditTransferInitiationV09();

            // Configura o cabeçalho do grupo (Group Header) com as informações globais do lote.
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

            // Define as instruções de pagamento, associando a conta devedora.
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

            // Itera sobre as transações para mapear cada pagamento individual no formato XSD.
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

                // Trata o campo de descrição garantindo um valor por omissão.
                txInfo.RmtInf = new ProjetoFinal.Models.Pain001.RemittanceInformation16();
                txInfo.RmtInf.Ustrd.Add(tx.Description ?? "Transferencia SEPA");
                pmtInf.CdtTrfTxInf.Add(txInfo);
            }

            // Anexa as instruções de pagamento ao documento principal.
            doc.CstmrCdtTrfInitn.PmtInf.Add(pmtInf);

            // Configura a serialização XML com o namespace correto para pain.001.
            var serializer = new XmlSerializer(typeof(ProjetoFinal.Models.Pain001.Document));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09");

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true
            };

            // Escreve o documento serializado em memória.
            using var memoryStream = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(memoryStream, xmlSettings))
            {
                serializer.Serialize(xmlWriter, doc, namespaces);
            }

            memoryStream.Position = 0;

            // Valida estruturalmente o ficheiro de acordo com o serviço XSD.
            var validationResult = _xmlValidator.ValidateXml(memoryStream, "PAIN001");
            if (!validationResult.IsValid)
            {
                string errorMessage = string.Join(" \n", validationResult.Errors);
                throw new InvalidOperationException($"O ficheiro XML gerado é inválido segundo as regras do banco (XSD):\n{errorMessage}");
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Gera o ficheiro XML SEPA no formato pain.008 (Débitos Diretos/Cobranças) a partir de um lote identificado.
        /// </summary>
        /// <param name="batchId">O identificador numérico único do lote de cobranças a processar.</param>
        /// <returns>Retorna um array de bytes contendo o documento XML estruturado e validado, pronto para exportação.</returns>
        /// <exception cref="KeyNotFoundException">Lançada caso o lote especificado não seja encontrado na base de dados.</exception>
        /// <exception cref="InvalidOperationException">Lançada caso a entidade emissora não esteja configurada ou o ficheiro gerado viole as regras de validação XSD.</exception>
        public async Task<byte[]> GeneratePain008XmlAsync(int batchId)
        {
            // Obtém o lote de cobranças da base de dados.
            var batch = await _context.TransferBatches
                .Include(b => b.Company)
                .Include(b => b.Transaction)
                    .ThenInclude(t => t.Contact)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
            {
                throw new KeyNotFoundException($"Transfer Batch with ID {batchId} not found!");
            }

            // Verifica a existência de dados configurados para a empresa.
            var myCompany = await _companyService.GetCompanyAsync();
            if (myCompany == null)
            {
                throw new InvalidOperationException("Erro: Cannot generate SEPA file. Configure your Company data first.");
            }

            // Inicializa a estrutura base do documento XML segundo o standard pain.008.
            var doc = new ProjetoFinal.Models.Pain008.Document();
            doc.CstmrDrctDbtInitn = new ProjetoFinal.Models.Pain008.CustomerDirectDebitInitiationV08();

            // Configura o cabeçalho do grupo.
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

            // Define as instruções de cobrança (Data de coleção e conta credora).
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

            // Itera sobre as transações de débito direto a efetuar.
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
                            // Define um valor de contingência para a referência do mandato caso se encontre nulo ou vazio.
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

                // Trata a informação de remessa, garantindo o preenchimento da descrição.
                txInfo.RmtInf = new ProjetoFinal.Models.Pain008.RemittanceInformation16();
                txInfo.RmtInf.Ustrd.Add(string.IsNullOrWhiteSpace(tx.Description) ? "Cobrança SEPA" : tx.Description);

                pmtInf.DrctDbtTxInf.Add(txInfo);
            }

            doc.CstmrDrctDbtInitn.PmtInf.Add(pmtInf);

            // Prepara a serialização utilizando os atributos e namespaces adequados ao pain.008.
            var serializer = new XmlSerializer(typeof(ProjetoFinal.Models.Pain008.Document));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08");

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
            };

            // Guarda o output em memória.
            using var memoryStream = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(memoryStream, xmlSettings))
            {
                serializer.Serialize(xmlWriter, doc, namespaces);
            }

            memoryStream.Position = 0;

            // Efetua a validação XSD para assegurar a conformidade.
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