using SharedModels.Models;
using SharedModels.QuickBooks.Models;

namespace task_14.Models
{
   
    public class Invoice
    {
        public int Id { get; set; } 
        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int InvoiceId { get; set; }
        public DateTime DueDate { get; set; }
        public string Store { get; set; }
        public string BillingAddress { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public bool SendLater { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    //public class InvoiceLineItem
    //{
    //    public int Id { get; set; }
    //    public string LineId { get; set; }
    //    public int InvoiceId { get; set; }
    //    public string ProductId { get; set; }
    //    public string Description { get; set; }
    //    public int Quantity { get; set; }
    //    public decimal Rate { get; set; }
    //    public decimal Amount { get; set; }
    //}
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



    public class InvoiceInputModel
    {
        public int CustomerId { get; set; }

        public int? InvoiceId { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Store { get; set; }
        public string BillingAddress { get; set; }
        public bool SendLater { get; set; }
        public List<InvoiceLineItemInputModel> LineItems { get; set; }
    }


    public class InvoiceCsvModel
    {
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string InvoiceDate { get; set; }
        public string DueDate { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string Quantity { get; set; }
        public string Rate { get; set; }
    }
    public class InvoiceLineItemInputModel
    {
        public int ProductId { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
    }

}
