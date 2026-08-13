using ProjetoFinal.Models.Pain001;
using System.Xml;
using System.Xml.Serialization;
using System;       
using System.IO;    
using System.Net.Sockets;
using Xunit;

namespace SepaXmlManager.tests
{
    public class Pain001GenerationTests
    {

        [Fact]
        public void Must_Generate_File_Pain001_With_Four_Payments()
        {
            
            //Documento e Cabeçalho
            var documento = new Document();
            documento.CstmrCdtTrfInitn = new CustomerCreditTransferInitiationV09();

            documento.CstmrCdtTrfInitn.GrpHdr = new GroupHeader85();
            documento.CstmrCdtTrfInitn.GrpHdr.MsgId = "MSG-TESTE-002";
            documento.CstmrCdtTrfInitn.GrpHdr.CreDtTm = DateTime.Now;
            documento.CstmrCdtTrfInitn.GrpHdr.NbOfTxs = "4";

            //Entidade Iniciadora (quem está a enviar o dinheiro) - neste caso, a empresa que está a pagar os salários
            documento.CstmrCdtTrfInitn.GrpHdr.InitgPty = new PartyIdentification135();
            documento.CstmrCdtTrfInitn.GrpHdr.InitgPty.Nm = "Empresa Exemplo Lda";

            //Instruções de Pagamento
            var infoPagamento = new PaymentInstruction30();
            infoPagamento.PmtInfId = "PAYMENT-INFO-001";
            infoPagamento.PmtMtd = PaymentMethod3Code.Trf;
            infoPagamento.ReqdExctnDt = new DateAndDateTime2Choice
            {
                Dt = DateTime.Now.Date,
                DtSpecified = true
            };

            // Identificação da entidade ordenante (devedor)
            infoPagamento.Dbtr = new PartyIdentification135();
            infoPagamento.Dbtr.Nm = "Empresa Exemplo Lda";

            // Identificação da conta bancária do devedor
            infoPagamento.DbtrAcct = new CashAccount38();
            infoPagamento.DbtrAcct.Id = new AccountIdentification4Choice();
            infoPagamento.DbtrAcct.Id.Iban = "PT50000201231234567890154";

            infoPagamento.ChrgBr = ChargeBearerType1Code.Slev;
            infoPagamento.ChrgBrSpecified = true;

            infoPagamento.DbtrAgt = new BranchAndFinancialInstitutionIdentification6();

            infoPagamento.DbtrAgt.FinInstnId = new FinancialInstitutionIdentification18();

            infoPagamento.DbtrAgt.FinInstnId.Bicfi = "CGDPTPLX";

            //Pagamentos (Credit Transfer Transaction Information)

            // Simulação de dados (dados hardcoded)
            var Transferencias = new []
            {
                (Id: "PAYMENT-001", NomeBeneficiario: "João Silva", IBANBeneficiario: "PT50000201231234567890155", Valor: 1100.00m, Descricao: "Vencimento Maio 2026"),
                (Id: "PAYMENT-002", NomeBeneficiario: "Maria Santos", IBANBeneficiario: "PT50000201231234567890156", Valor: 1200.00m, Descricao: "Vencimento Abril 2026"),
                (Id: "PAYMENT-003", NomeBeneficiario: "Carlos Oliveira", IBANBeneficiario: "PT50000201231234567890157", Valor: 1300.00m, Descricao: "Vencimento Fevereiro 2026"),
                (Id: "PAYMENT-004", NomeBeneficiario: "Ana Costa", IBANBeneficiario: "PT50000201231234567890158", Valor: 1400.00m, Descricao: "Vencimento Janeiro 2026")
            };

            // Iterar sobre a fonte de dados e mapear cada registo para as respetivas entidades do standard ISO 20022.
            foreach (var transf in Transferencias)
            {
                var transacao = new CreditTransferTransaction34();

                transacao.PmtId = new PaymentIdentification6();
                transacao.PmtId.EndToEndId = transf.Id;

                transacao.Amt = new AmountType4Choice();
                transacao.Amt.InstdAmt = new ActiveOrHistoricCurrencyAndAmount
                {
                    Ccy = "EUR",
                    Value = transf.Valor
                };

                transacao.Cdtr = new PartyIdentification135();
                transacao.Cdtr.Nm = transf.NomeBeneficiario;


                transacao.CdtrAcct = new CashAccount38();
                transacao.CdtrAcct.Id = new AccountIdentification4Choice { Iban = transf.IBANBeneficiario };


                transacao.RmtInf = new RemittanceInformation16();
                transacao.RmtInf.Ustrd.Add(transf.Descricao);

                infoPagamento.CdtTrfTxInf.Add(transacao);
            }

            // FECHO DO DOCUMENTO E SERIALIZAÇÃO XML (Ocorre apenas 1 vez!)


        documento.CstmrCdtTrfInitn.PmtInf.Add(infoPagamento);

        string destFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Test Outputs"));
        Directory.CreateDirectory(destFolder);
        string caminhoFicheiro = Path.Combine(destFolder, "Pain001_Lote4Transferencias.xml");

        var serializer = new XmlSerializer(typeof(Document));
        var settings = new XmlWriterSettings { Indent = true };

        using (var writer = XmlWriter.Create(caminhoFicheiro, settings))
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "urn:iso:std:iso:20022:tech:xsd:pain.001.001.09");
            serializer.Serialize(writer, documento, ns);
        }

        Assert.True(File.Exists(caminhoFicheiro));
        }


    }
}