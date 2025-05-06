using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.QuickBooks.Models
{
    public class QuickBooksProduct
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public string FullyQualifiedName { get; set; }
        public decimal UnitPrice { get; set; }
        public string Type { get; set; }
        public AccountRef IncomeAccountRef { get; set; }
        public string PurchaseDesc { get; set; }
        public decimal PurchaseCost { get; set; }
        public AccountRef ExpenseAccountRef { get; set; }
        public AccountRef AssetAccountRef { get; set; }

        public string? SyncToken { get; set; }
        public bool TrackQtyOnHand { get; set; }
        public decimal QtyOnHand { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class AccountRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class MetaData
    {
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }

    public class ProductQueryResponse
    {
        public List<QuickBooksProduct> Item { get; set; } = new List<QuickBooksProduct>();
    }

    public class ProductRoot
    {
        public ProductQueryResponse QueryResponse { get; set; } = new ProductQueryResponse();
    }

    public class QuickBooksProductInputModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        public string Description { get; set; }
        public string PurchaseDescription { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public bool? TrackQtyOnHand { get; set; }

        public decimal? QtyOnHand { get; set; }
        public DateTime? InvStartDate { get; set; }

        // Sales
        [Required]
        public decimal SalesUnitPrice { get; set; }

        [Required]
        public string IncomeAccountRef { get; set; }

        public string SalesTaxCodeRef { get; set; }

        public decimal? PurchaseUnitPrice { get; set; }
        public string ExpenseAccountRef { get; set; }

        public string AssetAccountRef { get; set; }
    }
}
