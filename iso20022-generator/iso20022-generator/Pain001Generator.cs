﻿using iso20022_generator.schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using iso20022_generator.entity;

namespace iso20022_generator
{
    public class Pain001Generator
    {
        private Document doc = new Document();
        private GroupHeader32CH grpHdr = new GroupHeader32CH(); // Index 1.0
        private PartyIdentification32CH_NameAndId initPty = new PartyIdentification32CH_NameAndId(); // Index 1.8
        private ContactDetails2CH ctctDtls = new ContactDetails2CH(); // Index 1.8
        private PaymentInstructionInformation3CH pmtInf1 = new PaymentInstructionInformation3CH(); // Index 2.0
        private PartyIdentification32CH dbtr = new PartyIdentification32CH(); // Index 2.19
        private CashAccount16CH_IdTpCcy dbtrAcct = new CashAccount16CH_IdTpCcy(); // Index 2.20
        private AccountIdentification4ChoiceCH dbtrAcctId = new AccountIdentification4ChoiceCH(); // Index 2.20 / Id
        private BranchAndFinancialInstitutionIdentification4CH_BicOrClrId dbtrAgt = new BranchAndFinancialInstitutionIdentification4CH_BicOrClrId(); // Index 2.21
        private FinancialInstitutionIdentification7CH_BicOrClrId finInstnIdDbtr = new FinancialInstitutionIdentification7CH_BicOrClrId(); // Index 2.21 / Financial Institution Identification

        /// <summary>
        /// Initializes a new generator which allows creating ISO20022-pain.001 files.
        /// </summary>
        /// <param name="init">Object with all the required information for setting up a new transaction document.</param>
        public Pain001Generator(Initialization init)
        {
            CustomerCreditTransferInitiationV03CH cstmrCdtTrfInitn = new CustomerCreditTransferInitiationV03CH();
            doc.CstmrCdtTrfInitn = cstmrCdtTrfInitn;

            // Level A
            cstmrCdtTrfInitn.GrpHdr = grpHdr; // Index 1.0
            grpHdr.MsgId = init.UniqueDocumentId; // Index 1.1 / Required for duplication check
            grpHdr.CreDtTm = DateTime.Now; // Index 1.2
            grpHdr.NbOfTxs = "0"; // Index 1.6
            grpHdr.CtrlSum = 0; // Index 1.7

            grpHdr.InitgPty = initPty; // Index 1.8

            initPty.Nm = init.SenderPartyName; // Index 1.8 - Name
            initPty.CtctDtls = ctctDtls; // Index 1.8 - Contact Details
            ctctDtls.Nm = "iso20022-Generator"; // Index 1.8 - Contact Details.Name
            ctctDtls.Othr = "1.3.0"; // Index 1.8 - Contact Details.Other


            // Level B
            cstmrCdtTrfInitn.PmtInf = new PaymentInstructionInformation3CH[1];
            cstmrCdtTrfInitn.PmtInf[0] = pmtInf1;

            pmtInf1.PmtInfId = "PmtInfId-1"; // Index 2.1
            pmtInf1.PmtMtd = PaymentMethod3Code.TRA; // Index 2.2
            pmtInf1.BtchBookg = true; // Index 2.3

            pmtInf1.ReqdExctnDt = init.ExecutionDate; // Index 2.17
            pmtInf1.Dbtr = dbtr;

            dbtr.Nm = init.SenderPartyName;

            pmtInf1.DbtrAcct = dbtrAcct;
            dbtrAcct.Id = dbtrAcctId;
            dbtrAcctId.Item = init.SenderIban; // Index 2.20 / Id / IBAN  Bezugs-Konto

            pmtInf1.DbtrAgt = dbtrAgt;

            // Add BIC only if is set to garantee the compatibility to the old version
            if (!string.IsNullOrEmpty(init.SenderBic))
                finInstnIdDbtr.BIC = init.SenderBic;

            dbtrAgt.FinInstnId = finInstnIdDbtr;

            

            // Level C
            pmtInf1.CdtTrfTxInf = new CreditTransferTransactionInformation10CH[0]; // Index 2.27
            
        }

        /// <summary>
        /// Adds a new transaction to the document
        /// </summary>
        /// <param name="receiver">Object with all the required information about the receiver of the new transaction</param>
        /// <param name="transaction">Object with all the required information about the transaction itself</param>
        public void AddTransaction(Receiver receiver, Transaction transaction)
        {
            CreditTransferTransactionInformation10CH cdtTrfTxInf = new CreditTransferTransactionInformation10CH(); // Index 2.27

            PaymentIdentification1 pmtId = new PaymentIdentification1(); // Index 2.28
            cdtTrfTxInf.PmtId = pmtId;
            pmtId.InstrId = "1-" + pmtInf1.CdtTrfTxInf.Length; // Index 2.29
            pmtId.EndToEndId = transaction.ReferenceIdentification; // Index 2.30

            PaymentTypeInformation19CH pmtTpInf = new PaymentTypeInformation19CH(); // Index 2.31
            cdtTrfTxInf.PmtTpInf = pmtTpInf;

            AmountType3Choice amt = new AmountType3Choice(); // Index 2.42
            cdtTrfTxInf.Amt = amt;

            ActiveOrHistoricCurrencyAndAmount currencyAndAmount = new ActiveOrHistoricCurrencyAndAmount(); // Index 2.43
            amt.Item = currencyAndAmount;
            currencyAndAmount.Ccy = transaction.CurrencyCode;
            currencyAndAmount.Value = transaction.Amount;

            BranchAndFinancialInstitutionIdentification4CH cdtrAgt = new BranchAndFinancialInstitutionIdentification4CH(); // Index 2.77
            cdtTrfTxInf.CdtrAgt = cdtrAgt;

            FinancialInstitutionIdentification7CH finInstnIdCdtr = new FinancialInstitutionIdentification7CH(); // Index 2.77 / Financial Institution Identification
            cdtrAgt.FinInstnId = finInstnIdCdtr;        

            PartyIdentification32CH_Name cdtr = new PartyIdentification32CH_Name(); // Index 2.79
            cdtTrfTxInf.Cdtr = cdtr;
            
            cdtr.Nm = receiver.Name; // Index 2.79 / Name
            PostalAddress6CH pstlAdr = new PostalAddress6CH(); // Index 2.79 / Postal Address
            cdtr.PstlAdr = pstlAdr;

            
            pstlAdr.StrtNm = receiver.StreetName; // Index 2.79 / Street Name

            if (!string.IsNullOrWhiteSpace(receiver.StreetNumber))
            {
                pstlAdr.StrtNm = receiver.StreetName + " " + receiver.StreetNumber; // Index 2.79 / Building Number
            }

            pstlAdr.PstCd = receiver.Zip; // Index 2.79 / Post Code
            pstlAdr.TwnNm = receiver.City; // Index 2.79 / Town Name
            pstlAdr.Ctry = receiver.CountryCode; // Index 2.79 / Country

            CashAccount16CH_Id cdtrAcct = new CashAccount16CH_Id(); // Index 2.80
            cdtTrfTxInf.CdtrAcct = cdtrAcct;

            cdtrAcct.Id = new AccountIdentification4ChoiceCH(); // Index 2.80 / Identification
            cdtrAcct.Id.Item = transaction.ReceiverIban; // Index 2.80 / Id / IBAN  Ziel-Konto

            AddNewCreditTransferTransactionInformation(pmtInf1.CdtTrfTxInf, cdtTrfTxInf);
            UpdateLevelA();
        }

        private void AddNewCreditTransferTransactionInformation(
            CreditTransferTransactionInformation10CH[] arrayToExtend, CreditTransferTransactionInformation10CH cdtTrfTxInf)
        {
            Array.Resize(ref arrayToExtend, arrayToExtend.Length + 1);
            arrayToExtend[arrayToExtend.Length - 1] = cdtTrfTxInf;
            pmtInf1.CdtTrfTxInf = arrayToExtend;
        }

        private void UpdateLevelA()
        {
            grpHdr.NbOfTxs = pmtInf1.CdtTrfTxInf.Length.ToString(); // Index 1.6
        }

        /// <summary>
        /// Serializes the generated document object to a given file destination
        /// </summary>
        /// <param name="fileName">Full path to the desired xml file</param>
        public void SavePain001ToDirectory(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                new XmlSerializer(typeof(Document)).Serialize(sw, doc);
            }
        }

        /// <summary>
        /// Returns the xml serialized document object
        /// </summary>
        /// <returns></returns>
        public string GetPain001String()
        {
            using (StringWriter sw = new StringWriter())
            {
                new XmlSerializer(typeof(Document)).Serialize(sw, doc);
                return sw.ToString();
            }
        }

        /// <summary>
        /// Allows direct access to the generated document object. This method is not thought to
        /// be used under normal conditions however your financial institude maybe requires
        /// setting some addittional properties.
        /// </summary>
        /// <returns>Document object which will be serialized to xml</returns>
        public Document getDocumentObject()
        {
            return doc;
        }
    }
}
