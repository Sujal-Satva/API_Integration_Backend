
namespace SharedModels.Models
{
    public class UnifiedInvoice
    {

        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Addresses { get; set; } // JSON string
        public string? LineItems { get; set; } // JSON string
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal AmountDue { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? LineAmountTypes { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string SourceSystem { get; set; }
    }

   
    public class InvoiceAddress
    {
        public string Type { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        public string Line4 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        
    }

    public class InvoiceLineItem
    {
        public string LineId { get; set; }
        public int? LineNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitAmount { get; set; }
        public string TaxType { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal LineAmount { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
    }


    public class UnifiedInvoiceInputModel
    {
        public string? ExternalId { get; set; }             // Optional: Used for updates
        public string InvoiceNumber { get; set; }
        public string? Reference { get; set; }
        public string Status { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }

        public List<InvoiceAddressInputModel> Addresses { get; set; } = new();
        public List<InvoiceLineItemInputModel> LineItems { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }

        
        public decimal TotalAmount { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public string? LineAmountTypes { get; set; }

        public DateTime UpdatedAt { get; set; }
        public string SourceSystem { get; set; }           // "QuickBooks" or "Xero"
    }


    public class InvoiceLineItemInputModel
    {
        public string ProductId { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }

        public string? AccountCode { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class InvoiceAddressInputModel
    {
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Type { get; set; }  // "Billing" or "Shipping"
    }


    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string? Reference { get; set; }
        public string? Status { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string BillingAddress { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public decimal AmountDue { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? LineAmountTypes { get; set; }
        public string SourceSystem { get; set; }
        public bool SendLater { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<InvoiceLineItem> LineItems { get; set; }
    }

}
