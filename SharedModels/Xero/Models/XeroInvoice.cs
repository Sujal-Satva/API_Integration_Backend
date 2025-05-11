
namespace SharedModels.Xero.Models
{
    public class XeroInvoice
    {
        public string Type { get; set; }
        public string InvoiceID { get; set; }
        public string InvoiceNumber { get; set; }
        public string Reference { get; set; }
        public List<XeroPayment> Payments { get; set; }
        public List<XeroCreditNote> CreditNotes { get; set; }
        public List<XeroPrepayment> Prepayments { get; set; }
        public List<XeroOverpayment> Overpayments { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountCredited { get; set; }
        public decimal CurrencyRate { get; set; }
        public bool IsDiscounted { get; set; }
        public bool HasAttachments { get; set; }
        public List<XeroInvoiceAddress> InvoiceAddresses { get; set; }
      
        public List<XeroInvoicePaymentService> InvoicePaymentServices { get; set; }
        public XeroContact Contact { get; set; }
        public string DateString { get; set; }
        public string Date { get; set; }
        public string DueDateString { get; set; }
        public string DueDate { get; set; }
        public string Status { get; set; }
        public string LineAmountTypes { get; set; }
        public List<XeroLineItem> LineItems { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal Total { get; set; }
        public string UpdatedDateUTC { get; set; }
        public string CurrencyCode { get; set; }
    }

    public class XeroPayment { }
    public class XeroCreditNote { }
    public class XeroPrepayment { }
    public class XeroOverpayment { }
    public class XeroInvoicePaymentService { }

    public class XeroContact
    {
        public string ContactID { get; set; }
        public string Name { get; set; }
        public List<XeroAddress> Addresses { get; set; }
        public List<XeroPhone> Phones { get; set; }
        public List<XeroContactGroup> ContactGroups { get; set; }
        public List<XeroContactPerson> ContactPersons { get; set; }
        public bool HasValidationErrors { get; set; }
    }
    public class XeroContactGroup { }
    public class XeroContactPerson { }
    public class XeroInvoiceAddress { }

    public class XeroLineItem
    {
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal UnitAmount { get; set; }
        public string TaxType { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineAmount { get; set; }
        public string AccountCode { get; set; }
        public XeroItem Item { get; set; }
        public List<XeroTracking> Tracking { get; set; }
        public decimal Quantity { get; set; }
        public string LineItemID { get; set; }
        public string AccountID { get; set; }
    }

    public class XeroItem
    {
        public string ItemID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class XeroTracking { }


    public class XeroInvoiceResponse
    {
        public List<XeroInvoice> Invoices { get; set; }
    }


}
