using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Xero.Models
{
    public class XeroBill
    {
        public string Type { get; set; } // e.g., "ACCPAY"
        public string InvoiceID { get; set; }
        public string InvoiceNumber { get; set; }
        public string Reference { get; set; }
       
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountCredited { get; set; }
        public Contact Contact { get; set; }
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string LineAmountTypes { get; set; }
        public List<LineItem> LineItems { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal Total { get; set; }
        public DateTime UpdatedDateUTC { get; set; }
        public string CurrencyCode { get; set; }
    }
    public class Contact
    {
        public string ContactID { get; set; }
        public string Name { get; set; }
       
    }
    public class XeroBillResponse
    {
        public List<XeroBill> Invoices { get; set; }
    }


    public class LineItem
    {
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal UnitAmount { get; set; }
        public string TaxType { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineAmount { get; set; }
        public string AccountCode { get; set; }
        public Item Item { get; set; }
        public decimal Quantity { get; set; }
        public string LineItemID { get; set; }
        public string AccountID { get; set; }
    }

    public class Item
    {
        public string ItemID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }




}
