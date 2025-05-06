    using Newtonsoft.Json;
    using SharedModels.Models;
    using SharedModels.QuickBooks.Models;

    namespace QuickBookService.Mappers
    {
        public static class InvoiceMapper
        {
        public static UnifiedInvoice MapQuickBooksInvoiceToUnified(QuickBooksInvoice qbInvoice)
        {
            if (qbInvoice == null) return null;

            // Safe parsing for dates
            DateTime invoiceDate = TryParseDate(qbInvoice.TxnDate);
            DateTime dueDate = TryParseDate(qbInvoice.DueDate);

            // Map addresses to JSON
            var addresses = new List<InvoiceAddress>();

            if (qbInvoice.BillAddr != null)
            {
                addresses.Add(new InvoiceAddress
                {
                    Type = "BILLING",
                    Line1 = qbInvoice.BillAddr.Line1,
                    Line2 = qbInvoice.BillAddr.Line2,
                    Line3 = qbInvoice.BillAddr.Line3,
                    Line4 = qbInvoice.BillAddr.Line4,
                    City = qbInvoice.BillAddr.City,
                    State = qbInvoice.BillAddr.CountrySubDivisionCode,
                    PostalCode = qbInvoice.BillAddr.PostalCode,
                });
            }

            if (qbInvoice.ShipAddr != null)
            {
                addresses.Add(new InvoiceAddress
                {
                    Type = "SHIPPING",
                    Line1 = qbInvoice.ShipAddr.Line1,
                    Line2 = qbInvoice.ShipAddr.Line2,
                    Line3 = qbInvoice.ShipAddr.Line3,
                    Line4 = qbInvoice.ShipAddr.Line4,
                    City = qbInvoice.ShipAddr.City,
                    State = qbInvoice.ShipAddr.CountrySubDivisionCode,
                    PostalCode = qbInvoice.ShipAddr.PostalCode,
                });
            }

            // Map line items to JSON
            var lineItems = new List<InvoiceLineItem>();

            if (qbInvoice.Line != null)
            {
                foreach (var line in qbInvoice.Line)
                {
                    // Skip subtotal lines
                    if (line.DetailType == "SubTotalLineDetail") continue;

                    if (line.DetailType == "SalesItemLineDetail" && line.SalesItemLineDetail != null)
                    {
                        lineItems.Add(new InvoiceLineItem
                        {
                            LineId = line.Id,
                            LineNumber = line.LineNum,
                            ItemCode = line.SalesItemLineDetail.ItemRef?.Value,
                            ItemName = line.SalesItemLineDetail.ItemRef?.Name,
                            Description = line.Description,
                            Quantity = line.SalesItemLineDetail.Qty,
                            UnitAmount = line.SalesItemLineDetail.UnitPrice,
                            TaxType = line.SalesItemLineDetail.TaxCodeRef?.Value,
                            LineAmount = line.Amount,
                            AccountCode = line.SalesItemLineDetail.ItemAccountRef?.Value,
                            AccountName = line.SalesItemLineDetail.ItemAccountRef?.Name
                        });
                    }
                }
            }

            // Calculate amount paid
            decimal amountPaid = qbInvoice.TotalAmt - qbInvoice.Balance;

            // Create the unified invoice
            return new UnifiedInvoice
            {
                ExternalId = qbInvoice.Id,
                InvoiceNumber = qbInvoice.DocNumber,
                Reference = qbInvoice.LinkedTxn?.FirstOrDefault()?.TxnId,
                Status = "DRAFT",
                CurrencyCode = qbInvoice.CurrencyRef?.Value,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                CustomerId = qbInvoice.CustomerRef?.Value,
                CustomerName = qbInvoice.CustomerRef?.Name,
                Addresses = JsonConvert.SerializeObject(addresses),
                LineItems = JsonConvert.SerializeObject(lineItems),
                Subtotal = qbInvoice.Line?.FirstOrDefault(l => l.DetailType == "SubTotalLineDetail")?.Amount ?? 0,
                TaxAmount = qbInvoice.TxnTaxDetail?.TotalTax ?? 0,
                TotalAmount = qbInvoice.TotalAmt,
                AmountDue = qbInvoice.Balance,
                AmountPaid = amountPaid,
                LineAmountTypes = "Exclusive",
                SourceSystem = "QuickBooks",
                UpdatedAt = DateTime.Parse(qbInvoice.MetaData.LastUpdatedTime)
            };
        }

        // Helper method to safely parse DateTime
        private static DateTime TryParseDate(string dateString)
        {
            DateTime parsedDate;
            return DateTime.TryParse(dateString, out parsedDate) ? parsedDate : DateTime.UtcNow;
        }


    }
}
