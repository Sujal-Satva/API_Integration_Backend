using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Xero.Models
{
    public class XeroProductInputModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
        public string PurchaseDescription { get; set; }

        [Required]
        public bool IsSold { get; set; }

        [Required]
        public bool IsPurchased { get; set; }

        // Optional if IsSold == false
        public decimal? SalesUnitPrice { get; set; }

        // Optional if IsPurchased == false
        public decimal? PurchaseUnitPrice { get; set; }

        public string SalesAccountCode { get; set; } // Required if IsSold
        public string PurchaseAccountCode { get; set; } // Required if IsPurchased

        public string SalesTaxType { get; set; }
        public string PurchaseTaxType { get; set; }

        [Required]
        public bool IsTrackedAsInventory { get; set; }
        public string InventoryAssetAccountCode { get; set; }
    }
}
