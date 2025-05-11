using SharedModels.QuickBooks.Models;
using SharedModels.Models;

namespace QuickBookService.Mappers
{
    public static class VendorMapper
    {
        public static List<UnifiedVendor> MapToUnifiedVendors(List<QuickBooksVendor> vendors)
        {
            if (vendors == null) return new List<UnifiedVendor>();

            return vendors.Select(v => new UnifiedVendor
            {
                ExternalId = v.Id,
                SourceSystem = "QuickBooks",
                DisplayName = v.DisplayName,
                CompanyName = v.CompanyName,
                Email = v.PrimaryEmailAddr?.Address,
                Phone = v.PrimaryPhone?.FreeFormNumber ?? v.Mobile?.FreeFormNumber,
                Website = v.WebAddr?.URI,
                Active = v.Active,
                Balance = v.Balance,
                Vendor1099 = v.Vendor1099,
                CreateTime = v.MetaData?.CreateTime ?? DateTime.UtcNow,
                LastUpdatedTime = v.MetaData?.LastUpdatedTime ?? DateTime.UtcNow
            }).ToList();
        }
    }
}
