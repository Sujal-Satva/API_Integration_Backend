using Newtonsoft.Json;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using SharedModels.Xero.Models;

namespace XeroService.Mappers
{
    public static class InvoiceMapper
    {
        public static UnifiedInvoice MapXeroInvoiceToUnified(XeroInvoice xeroInvoice)
        {
            if (xeroInvoice == null) return null;

            // Parse dates
            DateTime invoiceDate = DateTime.Parse(xeroInvoice.DateString);
            DateTime dueDate = DateTime.Parse(xeroInvoice.DueDateString);

            // Extract UTC time from Xero's "/Date(timestamp+0000)/" format
            string updatedUtcString = xeroInvoice.UpdatedDateUTC;
            DateTime updatedAt;

            if (updatedUtcString.StartsWith("/Date(") && updatedUtcString.EndsWith(")/"))
            {
                string timestampStr = updatedUtcString.Substring(6, updatedUtcString.IndexOf("+") - 6);
                long timestamp = long.Parse(timestampStr);
                updatedAt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
            }
            else
            {
                updatedAt = DateTime.UtcNow;
            }
            var addresses = new List<InvoiceAddress>();

            // Map line items to JSON
            var lineItems = new List<InvoiceLineItem>();

            if (xeroInvoice.LineItems != null)
            {
                foreach (var line in xeroInvoice.LineItems)
                {
                    lineItems.Add(new InvoiceLineItem
                    {
                        LineId = line.LineItemID,
                        ItemCode = line.ItemCode,
                        ItemName = line.Item?.Name,
                        Description = line.Description,
                        Quantity = line.Quantity,
                        UnitAmount = line.UnitAmount,
                        TaxType = line.TaxType,
                        TaxAmount = line.TaxAmount,
                        LineAmount = line.LineAmount,
                        AccountCode = line.AccountCode,
                        AccountName = null
                    });
                }
            }

            // Create the unified invoice
            return new UnifiedInvoice
            {
                ExternalId = xeroInvoice.InvoiceID,
                InvoiceNumber = xeroInvoice.InvoiceNumber,
                Reference = xeroInvoice.Reference,
                Status = xeroInvoice.Status,
                CurrencyCode = xeroInvoice.CurrencyCode,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                CustomerId = xeroInvoice.Contact?.ContactID,
                CustomerName = xeroInvoice.Contact?.Name,
                Addresses = JsonConvert.SerializeObject(addresses),
                LineItems = JsonConvert.SerializeObject(lineItems),
                Subtotal = xeroInvoice.SubTotal,
                TaxAmount = xeroInvoice.TotalTax,
                TotalAmount = xeroInvoice.Total,
                AmountDue = xeroInvoice.AmountDue,
                AmountPaid = xeroInvoice.AmountPaid,
                LineAmountTypes = xeroInvoice.LineAmountTypes,
                UpdatedAt = updatedAt,
                SourceSystem = "Xero"
            };
        }
        // Helper method to parse QuickBooks datetime
        private static DateTime ParseQuickBooksDateTime(string dateTimeStr)
        {
            if (string.IsNullOrEmpty(dateTimeStr))
                return DateTime.UtcNow;

            return DateTime.Parse(dateTimeStr);
        }

        
    }
}
