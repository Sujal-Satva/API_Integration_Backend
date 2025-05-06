using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickBookService.Mappers
{
    public static class CustomerMapper
    {

        public static List<UnifiedCustomer> MapQuickBooksCustomerToCommon(List<QuickBookCustomer> customers)
        {
            if (customers == null) return new List<UnifiedCustomer>();
            return customers.Select(c => new UnifiedCustomer
            {
                ExternalId = c.Id.ToString(),
                QuickBooksId = c.Id.ToString(),
                SourceSystem = "QuickBooks",
                DisplayName = c.DisplayName,
                FirstName = c.GivenName,
                LastName = c.FamilyName,
                CompanyName = c.CompanyName,
                EmailAddress = c.PrimaryEmailAddr?.Address,
                Website = null,
                Active = c.Active,
                AddressLine1 = c.BillAddr?.Line1,
                AddressLine2 = null,
                City = c.BillAddr?.City,
                Region = c.BillAddr?.CountrySubDivisionCode,
                PostalCode = c.BillAddr?.PostalCode,
                Country = "USA",
                AddressType = "Billing",
                PhoneType = "Primary",
                PhoneNumber = c.PrimaryPhone?.FreeFormNumber,
                LastUpdatedUtc = c.MetaData?.LastUpdatedTime.ToUniversalTime(),
            }).ToList();
        }

       
    }
}
