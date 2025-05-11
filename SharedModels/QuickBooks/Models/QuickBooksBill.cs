using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.QuickBooks.Models
{
    public class QBOBill
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string Domain { get; set; }
        public bool Sparse { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalAmt { get; set; }
        public string TxnDate { get; set; } 
        public string DueDate { get; set; }

        public CurrencyRef CurrencyRef { get; set; }
        public VendorRef VendorRef { get; set; }
        public APAccountRef APAccountRef { get; set; }
        public VendorAddr VendorAddr { get; set; }

        public MetaData MetaData { get; set; }

        public List<LinkedTxn> LinkedTxn { get; set; }
        public List<QBOBillLine> Line { get; set; }

        public SalesTermRef SalesTermRef { get; set; } // Optional
    }

    public class VendorRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class APAccountRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class CurrencyRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class VendorAddr
    {
        public string Id { get; set; }
        public string Line1 { get; set; }
        public string City { get; set; }
        public string CountrySubDivisionCode { get; set; }
        public string PostalCode { get; set; }
        public string Lat { get; set; }
        public string Long { get; set; }
    }

   
    public class LastModifiedByRef
    {
        public string Value { get; set; }
    }

    public class LinkedTxn
    {
        public string TxnId { get; set; }
        public string TxnType { get; set; }
    }

    public class SalesTermRef
    {
        public string Value { get; set; }
    }

    public class QuickBooksBillResponse
    {
        public QBOBill Bill { get; set; }
    }

    public class QBOBillLine
    {
        public string Id { get; set; }
        public int LineNum { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string DetailType { get; set; }

     
        public ItemBasedExpenseLineDetail ItemBasedExpenseLineDetail { get; set; }
        public AccountBasedExpenseLineDetail AccountBasedExpenseLineDetail { get; set; }

        public List<LinkedTxn> LinkedTxn { get; set; }
    }

    public class ItemBasedExpenseLineDetail
    {
        public string BillableStatus { get; set; }
        public ItemRef ItemRef { get; set; }
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public TaxCodeRef TaxCodeRef { get; set; }
    }

    public class AccountBasedExpenseLineDetail
    {
        public CustomerRef CustomerRef { get; set; }
        public AccountRef AccountRef { get; set; }
        public string BillableStatus { get; set; }
        public TaxCodeRef TaxCodeRef { get; set; }
    }

    public class CustomerRef
    {
        public string Value { get; set; }

        public string Name { get; set; }
    }


    public class ItemRef
    {
        public string Value { get; set; }

        public string Name { get; set; }
    }

    public class CreateBillRequest
    {
        public List<LineItem> Line { get; set; }
        public string DueDate { get; set; }
        public string TxnDate { get; set; }
        public Reference VendorRef { get; set; }
        public Reference APAccountRef { get; set; } // Required by QBO
        public Reference CurrencyRef { get; set; } // Optional unless multi-currency is on
    }

    public class LineItem
    {
        public string DetailType { get; set; } // "AccountBasedExpenseLineDetail" or "ItemBasedExpenseLineDetail"
        public decimal Amount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AccountBasedExpenseLineDetail2? AccountBasedExpenseLineDetail { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ItemBasedExpenseLineDetail2? ItemBasedExpenseLineDetail { get; set; }
    }

    public class AccountBasedExpenseLineDetail2
    {
        public Reference AccountRef { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Reference? CustomerRef { get; set; }
    }

    public class ItemBasedExpenseLineDetail2
    {
        public string BillableStatus { get; set; } // e.g., "NotBillable"
        public Reference ItemRef { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Qty { get; set; }
        public TaxCodeRef TaxCodeRef { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Reference? CustomerRef { get; set; }
    }

    public class Reference
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }
    }

    public class TaxCodeRef
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
