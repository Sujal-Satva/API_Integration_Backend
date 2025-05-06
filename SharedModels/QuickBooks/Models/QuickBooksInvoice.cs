using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.QuickBooks.Models
{
    public class QuickBooksInvoice
    {
       
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public QuickBooksMetaData MetaData { get; set; }
        public List<QuickBooksCustomField> CustomField { get; set; }
        public string DocNumber { get; set; }
        public string TxnDate { get; set; }
        public QuickBooksCurrencyRef CurrencyRef { get; set; }
        public List<QuickBooksLinkedTxn> LinkedTxn { get; set; }
        public List<QuickBooksLine> Line { get; set; }
        public QuickBooksTxnTaxDetail TxnTaxDetail { get; set; }
        public QuickBooksCustomerRef CustomerRef { get; set; }
        public QuickBooksCustomerMemo CustomerMemo { get; set; }
        public QuickBooksAddress BillAddr { get; set; }
        public QuickBooksAddress ShipAddr { get; set; }
        public bool FreeFormAddress { get; set; }
        public QuickBooksRef SalesTermRef { get; set; }
        public string DueDate { get; set; }
        public decimal TotalAmt { get; set; }
        public bool ApplyTaxAfterDiscount { get; set; }
        public string PrintStatus { get; set; }
        public string EmailStatus { get; set; }
        public QuickBooksEmail BillEmail { get; set; }
        public decimal Balance { get; set; }
    }

    public class QuickBooksMetaData
    {
        public string LastUpdatedTime { get; set; }
    }

    public class QuickBooksRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class QuickBooksCurrencyRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class QuickBooksCustomerRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class QuickBooksCustomerMemo
    {
        public string Value { get; set; }
    }

    public class QuickBooksEmail
    {
        public string Address { get; set; }
    }

    public class QuickBooksCustomField
    {
        public string DefinitionId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string StringValue { get; set; }
    }

    public class QuickBooksLinkedTxn
    {
        public string TxnId { get; set; }
        public string TxnType { get; set; }
    }

    public class QuickBooksLine
    {
        public string Id { get; set; }
        public int? LineNum { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public List<QuickBooksLinkedTxn> LinkedTxn { get; set; }
        public string DetailType { get; set; }
        public QuickBooksSalesItemLineDetail SalesItemLineDetail { get; set; }
        public QuickBooksSubTotalLineDetail SubTotalLineDetail { get; set; }
    }

    public class QuickBooksSalesItemLineDetail
    {
        public QuickBooksRef ItemRef { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Qty { get; set; }
        public QuickBooksRef ItemAccountRef { get; set; }
        public QuickBooksRef TaxCodeRef { get; set; }
    }

    public class QuickBooksSubTotalLineDetail { }

    public class QuickBooksTxnTaxDetail
    {
        public QuickBooksRef TxnTaxCodeRef { get; set; }
        public decimal TotalTax { get; set; }
        public List<QuickBooksTaxLine> TaxLine { get; set; }
    }

    public class QuickBooksTaxLine
    {
        public decimal Amount { get; set; }
        public string DetailType { get; set; }
        public QuickBooksTaxLineDetail TaxLineDetail { get; set; }
    }

    public class QuickBooksTaxLineDetail
    {
        public QuickBooksRef TaxRateRef { get; set; }
        public bool PercentBased { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal NetAmountTaxable { get; set; }
    }

    public class QuickBooksAddress
    {
        public string Id { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string Line4 { get; set; }
        public string City { get; set; }
        public string CountrySubDivisionCode { get; set; }
        public string PostalCode { get; set; }
        public string Lat { get; set; }
        public string Long { get; set; }
    }


    public class InvoiceQueryResponse
    {
        public List<QuickBooksInvoice> Invoice { get; set; } = new List<QuickBooksInvoice>();
    }

    public class InvoiceRoot
    {
        public InvoiceQueryResponse QueryResponse { get; set; } = new InvoiceQueryResponse();
    }

    
}
