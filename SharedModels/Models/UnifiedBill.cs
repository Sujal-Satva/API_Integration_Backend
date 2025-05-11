using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Models
{
    public class UnifiedBill
    {
        public int? Id { get; set; }
        public string? SourceSystem { get; set; }         // e.g., "QuickBooks", "Xero"
        public string? ExternalId { get; set; }           // Source system's bill ID

        public string? VendorName { get; set; }
        public string? VendorId { get; set; }

        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string? Currency { get; set; }
        public decimal? TotalAmount { get; set; }

        public string? Status { get; set; }
        public string? LineItems { get; set; }            
        public string? VendorDetails { get; set; }        

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }


    public class BillLineItem
    {
        public string LineId { get; set; }
        public int? LineNumber { get; set; }
        public string Description { get; set; }
        public decimal LineAmount { get; set; }
        public string DetailType { get; set; }

        public ItemBasedExpenseDetail ItemDetail { get; set; }
        public AccountBasedExpenseDetail AccountDetail { get; set; }
    }

    public class ItemBasedExpenseDetail
    {
        public string BillableStatus { get; set; }
        public string ItemCode { get; set; }

        public string? Description { get; set; }
        public string ItemName { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitAmount { get; set; }
        public string TaxType { get; set; }
        public decimal? TaxAmount { get; set; }
    }

    public class AccountBasedExpenseDetail
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string BillableStatus { get; set; }
        public string TaxType { get; set; }
    }


    public class BillRoot
    {
        public QueryResponse QueryResponse { get; set; }
    }

    public class QueryResponse
    {
        public List<QBOBill> Bill { get; set; }
    }

}
