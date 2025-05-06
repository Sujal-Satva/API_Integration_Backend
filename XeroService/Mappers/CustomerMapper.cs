using SharedModels.Models;
using SharedModels.Xero.Models;

namespace XeroService.Mappers
{
    public static class CustomerMapper
    {
        public static List<UnifiedCustomer> MapXeroProductsToUnifiedCustomers(List<XeroCustomer> xeroCustomers)
        {
            if (xeroCustomers == null) return new List<UnifiedCustomer>();

            return xeroCustomers.Select(xeroCustomer =>
            {
                var billingAddress = xeroCustomer.Addresses?.FirstOrDefault(addr => addr.AddressType == "STREET")
                    ?? xeroCustomer.Addresses?.FirstOrDefault();

                var primaryPhone = xeroCustomer.Phones?.FirstOrDefault(phone => phone.PhoneType == "DEFAULT");

                return new UnifiedCustomer
                {
                    ExternalId = xeroCustomer.ContactID,
                    XeroId = xeroCustomer.ContactID,
                    SourceSystem = "Xero",
                    DisplayName = xeroCustomer.Name,
                    FirstName = xeroCustomer.FirstName,
                    LastName = xeroCustomer.LastName,
                    CompanyName = null,
                    EmailAddress = xeroCustomer.EmailAddress,
                    Website = xeroCustomer.Website,
                    Active = xeroCustomer.ContactStatus == "ACTIVE",
                    AddressLine1 = billingAddress?.AddressLine1,
                    AddressLine2 = billingAddress?.AddressLine2,
                    City = billingAddress?.City,
                    Region = billingAddress?.Region,
                    PostalCode = billingAddress?.PostalCode,
                    Country = billingAddress?.Country,
                    AddressType = billingAddress?.AddressType,
                    PhoneType = primaryPhone?.PhoneType,
                    PhoneNumber = primaryPhone?.PhoneNumber,
                    LastUpdatedUtc = ParseXeroDate(xeroCustomer.UpdatedDateUTC)
                };
            }).ToList();
        }

        private static DateTime? ParseXeroDate(string? dateUtc)
        {
            if (string.IsNullOrWhiteSpace(dateUtc)) return null;
            return DateTime.TryParse(dateUtc, out var parsed) ? parsed.ToUniversalTime() : null;
        }
    }
}
