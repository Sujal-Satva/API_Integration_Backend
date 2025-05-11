    using Newtonsoft.Json;
    using SharedModels.Models;
    using SharedModels.Xero.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace XeroService.Mappers
    {
        public static class BillMapper
        {
            public static UnifiedBill MapXeroBillToUnifiedBill(XeroBill xeroBill)
            {
                var unifiedBill = new UnifiedBill
                {
                    // Mapping basic bill details
                    SourceSystem = "Xero",
                    ExternalId = xeroBill.InvoiceID,
                    VendorName = xeroBill.Contact?.Name ?? string.Empty,
                    VendorId = xeroBill.Contact?.ContactID ?? string.Empty,
                    IssueDate = xeroBill.Date,
                    DueDate = xeroBill.DueDate,
                    Currency = xeroBill.CurrencyCode,
                    TotalAmount = xeroBill.Total,
                    Status = xeroBill.Status,
                    CreatedAt = DateTime.UtcNow,  
                    UpdatedAt = DateTime.UtcNow, 

                    LineItems = MapLineItems(xeroBill.LineItems),
                    VendorDetails = MapVendorDetails(xeroBill.Contact)
                };

                return unifiedBill;
            }


        private static string MapLineItems(List<LineItem> lineItems)
        {
            var billLineItems = lineItems.Select(line => new BillLineItem
            {
                LineId = line.LineItemID?.ToString() ?? Guid.NewGuid().ToString(), // Ensure it's a string
                Description = line.Description ?? string.Empty,
                LineAmount = line.LineAmount,
                DetailType = "ItemBasedExpenseDetail", 
                ItemDetail = line.Item != null ? new ItemBasedExpenseDetail
                {
                    ItemCode = line.Item.Code ?? string.Empty,
                    //Description = line.Item.Name ?? string.Empty,
                    UnitAmount=line.UnitAmount,
                    Quantity=line.Quantity,
                    ItemName=line.Item.Name
                } : null,
                AccountDetail = new AccountBasedExpenseDetail
                {
                    AccountCode = line.AccountCode ?? string.Empty
                }
            }).ToList();

            return JsonConvert.SerializeObject(billLineItems, Formatting.None);
        }

        private static string MapVendorDetails(Contact contact)
        {
            if (contact == null)
                return string.Empty;

            var vendorDetails = new
            {
                Name = contact.Name ?? string.Empty,
                ContactID = contact.ContactID ?? string.Empty
            };

            return JsonConvert.SerializeObject(vendorDetails, Formatting.None);
        }


        public static List<UnifiedBill> MapXeroBillsToUnifiedBills(List<XeroBill> xeroBills)
            {
                return xeroBills.Select(MapXeroBillToUnifiedBill).ToList();
            }


    }
}
