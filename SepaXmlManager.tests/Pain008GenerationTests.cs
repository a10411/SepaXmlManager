using ProjetoFinal.Models.Pain008;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.IO;
using System.Net.Sockets;
using Xunit;




namespace SepaXmlManager.tests
{
    public class Pain008GenerationTests
    {
        [Fact]
        public void Must_Generate_File_Pain008_With_Four_Collections()
        {
            // Documento e Cabeçalho
            var documento = new Document();
            documento.CstmrDrctDbtInitn = new CustomerDirectDebitInitiationV08();

            documento.CstmrDrctDbtInitn.GrpHdr = new GroupHeader83();
            documento.CstmrDrctDbtInitn.GrpHdr.MsgId = "MSG-TESTE-008";
            documento.CstmrDrctDbtInitn.GrpHdr.CreDtTm = DateTime.Now;
            documento.CstmrDrctDbtInitn.GrpHdr.NbOfTxs = "4";

            documento.CstmrDrctDbtInitn.GrpHdr.CtrlSum = 5000.00m;
            documento.CstmrDrctDbtInitn.GrpHdr.CtrlSumSpecified = true;

            //Entidade que está a iniciar a transação (empresa que vai receber o dinheiro)
            documento.CstmrDrctDbtInitn.GrpHdr.InitgPty = new PartyIdentification135();
            documento.CstmrDrctDbtInitn.GrpHdr.InitgPty.Nm = "Empresa Exemplo Lda";

            //Instruções de Cobrança
            var infoCobrança = new PaymentInstruction29();
            infoCobrança.PmtInfId = "COLLECTION-INFO-001";  
            infoCobrança.PmtMtd = PaymentMethod2Code.Dd;
            infoCobrança.ReqdColltnDt = DateTime.Now.AddDays(5 ).Date; // Data de cobrança

            infoCobrança.PmtTpInf = new PaymentTypeInformation29();
            infoCobrança.PmtTpInf.SeqTp = SequenceType3Code.Rcur; // Cobrança recorrente

            //Identificação do credor (entidade que vai receber o dinheiro)
            var identCredor = new GenericPersonIdentification1();
            identCredor.Id = "PT99ZZZ123456789"; // Identificação do credor (exemplo)
            var scheme = new PersonIdentificationSchemeName1Choice { Prtry = "SEPA" };
            identCredor.SchmeNm = scheme;
            //Cobranças
            var Cobrancas = new[]
                        {
                (Id: "COB-001", Cliente: "João Silva", IBANCliente: "PT50000201231234567890155", BIC: "BPIXPTPL", Valor: 1100.00m, Descricao: "Mensalidade Ginásio", Mandato: "MAND-JS-001", DataMandato: new DateTime(2023, 01, 15)),
                (Id: "COB-002", Cliente: "Maria Santos", IBANCliente: "PT50000201231234567890156", BIC: "MILLPTPL", Valor: 1200.00m, Descricao: "Mensalidade Ginásio", Mandato: "MAND-MS-002", DataMandato: new DateTime(2024, 05, 10)),
                (Id: "COB-003", Cliente: "Carlos Oliveira", IBANCliente: "PT50000201231234567890157", BIC: "NOVBPTPL", Valor: 1300.00m, Descricao: "Aulas Personalizadas", Mandato: "MAND-CO-003", DataMandato: new DateTime(2025, 02, 20)),
                (Id: "COB-004", Cliente: "Ana Costa", IBANCliente: "PT50000201231234567890158", BIC: "CGDPTPLX", Valor: 1400.00m, Descricao: "Plano Anual", Mandato: "MAND-AC-004", DataMandato: new DateTime(2026, 01, 05))
            };

            foreach (var cob in Cobrancas)
            {
                var transacao = new DirectDebitTransactionInformation23(); 

                transacao.PmtId = new PaymentIdentification6();
                transacao.PmtId.EndToEndId = cob.Id;

                // Ao contrário do pain.001, o montante no pain.008 costuma ser direto no InstdAmt
                transacao.InstdAmt = new ActiveOrHistoricCurrencyAndAmount
                {
                    Ccy = "EUR",
                    Value = cob.Valor
                };

                // DADOS DO MANDATO (A autorização assinada)
                transacao.DrctDbtTx = new DirectDebitTransaction10();
                transacao.DrctDbtTx.MndtRltdInf = new MandateRelatedInformation14();
                transacao.DrctDbtTx.MndtRltdInf.MndtId = cob.Mandato;
                transacao.DrctDbtTx.MndtRltdInf.DtOfSgntr = cob.DataMandato;
                transacao.DrctDbtTx.MndtRltdInf.DtOfSgntrSpecified = true;

                // DADOS DO DEVEDOR (O teu Cliente)
                transacao.DbtrAgt = new BranchAndFinancialInstitutionIdentification6();
                transacao.DbtrAgt.FinInstnId = new FinancialInstitutionIdentification18();
                transacao.DbtrAgt.FinInstnId.Bicfi = cob.BIC;

                transacao.Dbtr = new PartyIdentification135();
                transacao.Dbtr.Nm = cob.Cliente;

                transacao.DbtrAcct = new CashAccount38();
                transacao.DbtrAcct.Id = new AccountIdentification4Choice { Iban = cob.IBANCliente };

                transacao.RmtInf = new RemittanceInformation16();
                transacao.RmtInf.Ustrd.Add(cob.Descricao);

                infoCobrança.DrctDbtTxInf.Add(transacao);
            }

            // -------------------------------------------------------------------------
            // FECHO E SERIALIZAÇÃO XML
            // -------------------------------------------------------------------------
            documento.CstmrDrctDbtInitn.PmtInf.Add(infoCobrança);

            string destFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Test Outputs"));
            Directory.CreateDirectory(destFolder);
            string caminhoFicheiro = Path.Combine(destFolder, "Pain008_Lote4Cobrancas.xml");

            var serializer = new XmlSerializer(typeof(Document));
            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(caminhoFicheiro, settings))
            {
                var ns = new XmlSerializerNamespaces();
                // ATENÇÃO AO NAMESPACE OFICIAL DO PAIN.008
                ns.Add("", "urn:iso:std:iso:20022:tech:xsd:pain.008.001.08"); // (Pode ser .02, .08, dependendo do teu XSD)

                serializer.Serialize(writer, documento, ns);
            }

            Assert.True(File.Exists(caminhoFicheiro));
        }
    }
}