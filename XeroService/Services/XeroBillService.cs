using Newtonsoft.Json;
using SharedModels.Models;
using SharedModels.Xero.Models;
using XeroService.Interfaces;
using XeroService.Mappers;

namespace XeroService.Services
{
    public class XeroBillService : IXeroBillService
    {
        private readonly IXeroApiService _xeroApiService;

        public XeroBillService(IXeroApiService xeroApiService)
        {
            _xeroApiService = xeroApiService;
        }
        private DateTime GetLastSyncedDate(string syncingInfoJson, string key)
        {
            if (string.IsNullOrEmpty(syncingInfoJson)) return new DateTime(2000, 1, 1);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(syncingInfoJson);
            if (dict != null && dict.TryGetValue(key, out var value))
            {
                if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    return parsed;
                }
            }
            return new DateTime(2000, 1, 1);
        }

        public async Task<CommonResponse<object>> FetchBillsFromXero(ConnectionModal connection)
        {
            try
            {
                int page = 1;
                var allBills = new List<XeroBill>();
                // Get the last synced date for bills
                DateTime lastSynced = GetLastSyncedDate(connection.SyncingInfo, "Bills");
                DateTime bufferedTime = lastSynced.ToUniversalTime().AddMinutes(-10);
                string baseFilter = $"Type==\"ACCPAY\"";
                if (bufferedTime > new DateTime(2000, 1, 1))
                {
                    baseFilter += $"&&UpdatedDateUTC>=DateTime({bufferedTime:yyyy, MM, dd, HH, mm, ss})";
                }
                while (true)
                {
                    string url = $"/Invoices?page={page}&where={Uri.EscapeDataString(baseFilter)}";

                    var apiResponse = await _xeroApiService.XeroGetRequest(url, connection);
                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch bills from Xero", apiResponse.Data);
                    var final = JsonConvert.DeserializeObject<XeroBillResponse>(apiResponse.Data.ToString());
                    var bills = final?.Invoices;
                    if (bills == null || bills.Count == 0) break;
                    allBills.AddRange(bills);
                    if (bills.Count < 100)
                        break;
                    page++;
                }

                
                if (allBills.Count == 0)
                    return new CommonResponse<object>(200, "All bills are already synced", null);

               
                var unifiedBills = BillMapper.MapXeroBillsToUnifiedBills(allBills);

               
                return new CommonResponse<object>(200, "Bills fetched and mapped successfully", unifiedBills);
            }
            catch (Exception ex)
            {

                return new CommonResponse<object>(500, "Error occurred while syncing bills from Xero", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> AddBill(ConnectionModal connection, UnifiedInvoiceInputModel input)
        {
            try
            {

                var invoicePayload = new
                {
                    Type = "ACCPAY",
                    Contact = new { Id = input.CustomerId, Name = input.CustomerName },
                    Date = input.InvoiceDate.ToString("yyyy-MM-dd"), // Invoice date
                    DueDate = input.DueDate.ToString("yyyy-MM-dd"), // Due date
                    LineItems = input.LineItems.Select(li => new
                    {
                        Description = li.Description,
                        Quantity = li.Quantity,
                        UnitAmount = li.Rate,
                        AccountCode = li.AccountCode,
                        TaxType = "OUTPUT",
                        ItemCode = li.ProductId
                    }).ToList(),
                };


                var apiResponse = await _xeroApiService.XeroPostRequest("/Invoices", invoicePayload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var final = JsonConvert.DeserializeObject<XeroBillResponse>(apiResponse.Data.ToString());
                    var bill = final?.Invoices?.FirstOrDefault();
                    var unifiedBill = BillMapper.MapXeroBillToUnifiedBill(bill);
                    return new CommonResponse<object>(200, "Invoice created successfully in Xero", new List<UnifiedBill> { unifiedBill });
                }
                return new CommonResponse<object>(apiResponse.Status, "Failed to create invoice in Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding invoice to Xero", ex.Message);
            }
        }

    }
}
