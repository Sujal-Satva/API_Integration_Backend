using Newtonsoft.Json;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;

namespace QuickBookService.Mappers
{
    public static class BillMapper
    {
        public static List<UnifiedBill> MapQuickBooksBillToCommon(List<QBOBill> bills)
        {
            if (bills == null) return new List<UnifiedBill>();

            return bills.Select(b => new UnifiedBill
            {
                ExternalId = b.Id,
                SourceSystem = "QuickBooks",
                VendorName = b.VendorRef?.Name,
                VendorId = b.VendorRef?.Value,
                IssueDate = DateTime.TryParse(b.TxnDate, out var txnDate) ? txnDate : DateTime.MinValue,
                DueDate = DateTime.TryParse(b.DueDate, out var dueDate) ? dueDate : (DateTime?)null,
                Currency = b.CurrencyRef?.Name,
                TotalAmount = b.TotalAmt,
                Status = "ACTIVE", // Assuming all bills are open for simplicity, adjust as needed.
                LineItems = JsonConvert.SerializeObject(MapQuickBooksBillLineToCommon(b.Line)),
                VendorDetails = b.VendorAddr != null
                    ? $"{b.VendorAddr.Line1}, {b.VendorAddr.City}, {b.VendorAddr.CountrySubDivisionCode}, {b.VendorAddr.PostalCode}"
                    : null,
                CreatedAt = b.MetaData.CreateTime, // Adjust according to your requirements
                UpdatedAt = b.MetaData.LastUpdatedTime // Adjust according to your requirements
            }).ToList();
        }


        public static List<BillLineItem> MapQuickBooksBillLineToCommon(List<QBOBillLine> lines)
        {
            if (lines == null) return new List<BillLineItem>();

            return lines.Select(static l => new BillLineItem
            {
                LineId = l.Id,
                LineNumber = l.LineNum,
                Description = l.Description,
                LineAmount = l.Amount,
                DetailType = l.DetailType,

                ItemDetail = l.DetailType == "ItemBasedExpenseLineDetail" && l.ItemBasedExpenseLineDetail != null
                    ? new ItemBasedExpenseDetail
                    {
                        BillableStatus = l.ItemBasedExpenseLineDetail.BillableStatus,
                        ItemCode = l.ItemBasedExpenseLineDetail.ItemRef?.Value,
                        ItemName = l.ItemBasedExpenseLineDetail.ItemRef?.Name,
                        Quantity = l.ItemBasedExpenseLineDetail.Qty,
                        UnitAmount = l.ItemBasedExpenseLineDetail.UnitPrice,
                        TaxType = l.ItemBasedExpenseLineDetail.TaxCodeRef?.Value,
                        TaxAmount = 0 
                    }
                    : null,

                AccountDetail = l.DetailType == "AccountBasedExpenseLineDetail" && l.AccountBasedExpenseLineDetail != null
                    ? new AccountBasedExpenseDetail
                    {
                        CustomerId = l.AccountBasedExpenseLineDetail.CustomerRef?.Value,
                        CustomerName = l.AccountBasedExpenseLineDetail.CustomerRef?.Name,
                        AccountCode = l.AccountBasedExpenseLineDetail.AccountRef?.Value,
                        AccountName = l.AccountBasedExpenseLineDetail.AccountRef?.Name,
                        BillableStatus = l.AccountBasedExpenseLineDetail.BillableStatus,
                        TaxType = l.AccountBasedExpenseLineDetail.TaxCodeRef?.Value
                    }
                    : null
            }).ToList();
        }

    }
}
