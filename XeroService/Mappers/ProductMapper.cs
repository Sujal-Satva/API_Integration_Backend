using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedModels.QuickBooks.Models;
//using task_14.Models;
using SharedModels.Models;
using SharedModels.Xero.Models;

namespace QuickBookService.Mappers
{
    public static class ProductMapper
    {
        public static List<UnifiedItem> MapXeroProductsToUnifiedItems(List<XeroProduct> products)
        {
            if (products == null) return new List<UnifiedItem>();

            return products.Select(p => new UnifiedItem
            {
                ExternalId = p.ItemId,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                PurchaseDescription = p.PurchaseDescription,
                SalesUnitPrice = p.SalesDetails?.UnitPrice ?? 0,
                PurchaseUnitPrice = p.PurchaseDetails?.UnitPrice ?? 0,
                IncomeAccountName = p.SalesDetails?.AccountCode,
                ExpenseAccountName = p.PurchaseDetails?.COGSAccountCode,
                AssetAccountName = p.InventoryAssetAccountCode,
                IsTrackedAsInventory = p.IsTrackedAsInventory,
                QuantityOnHand = p.QuantityOnHand,
                IsActive = (p.IsSold && p.IsPurchased) || true,
                SourceSystem = "Xero",  
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow   
            }).ToList();
        }
    }
}
