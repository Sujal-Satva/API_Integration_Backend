using Newtonsoft.Json;
using QuickBookService.Interfaces;
using QuickBookService.Mappers;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;

namespace QuickBookService.Services
{
    public class QuickBooksBillService : IQuickBooksBillService
    {
        private readonly IQuickBooksApiService _quickBooksApiService;
        public QuickBooksBillService(IQuickBooksApiService quickBooksApiService)
        {
            _quickBooksApiService = quickBooksApiService;
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

        public async Task<CommonResponse<object>> FetchBillsFromQuickBooks(ConnectionModal connection)
        {
            try
            {
                int startPosition = 1;
                const int maxResults = 1000;
                var lastSync = GetLastSyncedDate(connection.SyncingInfo, "Bills");

                var allBills = new List<QBOBill>();

                while (true)
                {
                    string query = $"SELECT * FROM Bill WHERE Metadata.LastUpdatedTime > '{lastSync:yyyy-MM-ddTHH:mm:ssZ}' " +
                                   $"STARTPOSITION {startPosition} MAXRESULTS {maxResults}";

                    var apiResponse = await _quickBooksApiService.QuickBooksGetRequest(
                        $"query?query={Uri.EscapeDataString(query)}", connection);

                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch bills", apiResponse.Data);

                    var result = JsonConvert.DeserializeObject<BillRoot>(apiResponse.Data.ToString());
                    var bills = result?.QueryResponse?.Bill;

                    if (bills == null || bills.Count == 0)
                        break;

                    allBills.AddRange(bills);

                    if (bills.Count < maxResults)
                        break;

                    startPosition += maxResults;
                }

                if (allBills.Count == 0)
                    return new CommonResponse<object>(200, "All Bills are already Synced", null);
                var unifiedBills = BillMapper.MapQuickBooksBillToCommon(allBills);

                return new CommonResponse<object>(200, "Bills fetched and mapped successfully", unifiedBills);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing Bills", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> AddBillToQuickBooks(CreateBillRequest billDto, ConnectionModal connection)
        {
               try
                {
                        
                        var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("bill", billDto, connection);
                        if (apiResponse.Status == 200 || apiResponse.Status == 201)
                        {

                            var result = JsonConvert.DeserializeObject<QuickBooksBillResponse>(apiResponse.Data.ToString())?.Bill;
                                
                            if (result != null)
                            {
                                var mapped = BillMapper.MapQuickBooksBillToCommon(new List<QBOBill> { result});
                                return new CommonResponse<object>(200, "Bill created successfully", mapped);
                            }
                        }
                        return new CommonResponse<object>(apiResponse.Status, "Failed to create bill", apiResponse.Data);
                    }
                    catch (Exception ex)
                    {
                        return new CommonResponse<object>(500, "Error occurred while adding bill", ex.Message);
                    }
        }


    }
}
