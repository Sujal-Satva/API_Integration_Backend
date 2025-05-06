using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Xero.Models
{
    public class XeroProduct
    {
   
        public string ItemId { get; set; }

      
        public string Code { get; set; }

        public string Description { get; set; }

      
        public string PurchaseDescription { get; set; }

        
        public string UpdatedDateUTC { get; set; }

        public XeroProductDetails PurchaseDetails { get; set; }

        public XeroProductDetails SalesDetails { get; set; }

      
        public string Name { get; set; }

    
        public bool IsTrackedAsInventory { get; set; }

      
        public string InventoryAssetAccountCode { get; set; }

       
        public decimal TotalCostPool { get; set; }
 
        public decimal QuantityOnHand { get; set; }

       
        public bool IsSold { get; set; }

       
        public bool IsPurchased { get; set; }
    }

    public class XeroProductDetails
    {
       
        public decimal UnitPrice { get; set; }

        
        public string COGSAccountCode { get; set; }

       
        public string TaxType { get; set; }

        
        public string AccountCode { get; set; }
    }

    public class XeroProductResponse
    {
        public List<XeroProduct> Items { get; set; }
    }
}
