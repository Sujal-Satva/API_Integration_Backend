
using SharedModels.QuickBooks.Models;
using SharedModels.Models;

namespace QuickBookService.Mappers
{
    public static class ProductMapper
    {
        public static List<UnifiedItem> MapQuickBooksProductsToUnifiedItems(List<QuickBooksProduct> products)
        {
            if (products == null) return new List<UnifiedItem>();

            return products.Select(p => new UnifiedItem
            {
                ExternalId = p.Id,
                Name = p.Name,
                Code = p.FullyQualifiedName,
                Description = p.Description,
                PurchaseDescription = p.PurchaseDesc,
                SalesUnitPrice = p.UnitPrice,
                PurchaseUnitPrice = p.PurchaseCost,
                IncomeAccountName = p.IncomeAccountRef?.Name,
                ExpenseAccountName = p.ExpenseAccountRef?.Name,
                AssetAccountName = p.AssetAccountRef?.Name,
                IsTrackedAsInventory = p.TrackQtyOnHand,
                QuantityOnHand = p.QtyOnHand,
                IsActive = p.Active,
                SourceSystem = "QuickBooks",
                CreatedAt = p.MetaData?.CreateTime ?? DateTime.UtcNow,
                UpdatedAt = p.MetaData?.LastUpdatedTime ?? DateTime.UtcNow
            }).ToList();
        }
    }
}
