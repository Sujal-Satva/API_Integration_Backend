using Newtonsoft.Json;
using QuickBookService.Interfaces;
using QuickBookService.Mappers;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickBookService.Services
{
    public class QuickBooksCustomerService:IQuickBooksCustomerService
    {
        private readonly IQuickBooksApiService _quickBooksApiService;
        public QuickBooksCustomerService(IQuickBooksApiService quickBooksApiService)
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

        public async Task<CommonResponse<object>> FetchCustomersFromQuickBooks(ConnectionModal connection)
        {
            try
            {
                int startPosition = 1;
                const int maxResults = 1000;
                var lastSync = GetLastSyncedDate(connection.SyncingInfo, "Customers");

                var allCustomers = new List<QuickBookCustomer>();

                while (true)
                {
                    string query = $"SELECT * FROM Customer WHERE Metadata.LastUpdatedTime > '{lastSync:yyyy-MM-ddTHH:mm:ssZ}' " +
                                   $"STARTPOSITION {startPosition} MAXRESULTS {maxResults}";
                    var apiResponse = await _quickBooksApiService.QuickBooksGetRequest(
                        $"query?query={Uri.EscapeDataString(query)}", connection);
                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch products", apiResponse.Data);
                    var result = JsonConvert.DeserializeObject<CustomerRoot>(apiResponse.Data.ToString());
                    var customers = result?.QueryResponse?.Customer;
                    if (customers == null || customers.Count == 0)
                        break;
                    allCustomers.AddRange(customers);
                    if (customers.Count < maxResults)
                        break;
                    startPosition += maxResults;
                }

                if (allCustomers.Count == 0)
                    return new CommonResponse<object>(200, "All Customer are already Synced", null);
                var unifiedItems = CustomerMapper.MapQuickBooksCustomerToCommon(allCustomers);
                return new CommonResponse<object>(200, "Customer fetched and mapped successfully", unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing Customers", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> AddItem(ConnectionModal connection, CustomerInputModel inputModel)
        {
            try
            {
                var payload = new
                {
                    DisplayName = inputModel.DisplayName,
                    GivenName = inputModel.GivenName ?? "",
                    FamilyName = inputModel.FamilyName ?? "",
                    PrimaryEmailAddr = new { Address = inputModel.EmailAddress ?? "" },
                    PrimaryPhone = new { FreeFormNumber = inputModel.PhoneNumber ?? "" },
                    BillAddr = new
                    {
                        Line1 = inputModel.AddressLine1 ?? "",
                        City = inputModel.City ?? "",
                        CountrySubDivisionCode = inputModel.CountrySubDivisionCode ?? "",
                        PostalCode = inputModel.PostalCode ?? ""
                    }
                };

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("customer", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = apiResponse.Data;
                    var final = JsonConvert.DeserializeObject<QuickBooksCustomerResponse>(apiResponse.Data.ToString());
                    var product = final?.Customer;

                    if (product != null)
                    {
                        var productList = new List<QuickBookCustomer> { product };
                        var unifiedItems = CustomerMapper.MapQuickBooksCustomerToCommon(productList);
                        return new CommonResponse<object>(200, "Product added successfully", unifiedItems);
                    }

                    return new CommonResponse<object>(200, "Product added but could not parse product details", null);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to add product", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding product", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> GetItemById(ConnectionModal connection, string itemId)
        {
            try
            {
                var apiResponse = await _quickBooksApiService.QuickBooksGetRequest($"customer/{itemId}", connection);

                if (apiResponse.Status == 200)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksCustomerResponse>(apiResponse.Data.ToString());
                    return new CommonResponse<object>(200, "Customer fetched successfully", result.Customer);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to fetch Customer", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while fetching Customer", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditCustomer(ConnectionModal connection, string itemId, CustomerInputModel inputModel)
        {
            try
            {
                var syncTokenResult = await GetItemById(connection, itemId);
                if (syncTokenResult.Status != 200 || syncTokenResult.Data == null)
                {
                    return new CommonResponse<object>(syncTokenResult.Status, "Failed to fetch SyncToken", syncTokenResult.Data);
                }

                var existingProduct = (QuickBookCustomer)syncTokenResult.Data;
                var payload = new
                {
                    Id = existingProduct.Id,
                    SyncToken = existingProduct.SyncToken,
                    DisplayName = inputModel.DisplayName,
                    PrimaryEmailAddr = new { Address = inputModel.EmailAddress },
                    PrimaryPhone = new { FreeFormNumber = inputModel.PhoneNumber },
                    BillAddr = new
                    {
                        Line1 = inputModel.AddressLine1,
                        City = inputModel.City,
                        CountrySubDivisionCode = inputModel.CountrySubDivisionCode,
                        PostalCode = inputModel.PostalCode,
                    }
                };

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("customer", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksCustomerResponse>(apiResponse.Data.ToString());
                    var unifiedItems = CustomerMapper.MapQuickBooksCustomerToCommon(new List<QuickBookCustomer> { result.Customer });
                    return new CommonResponse<object>(200, "Customer updated successfully", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update Customer", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating Customer", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> UpdateCustomerStatus(ConnectionModal connection, string id,string status)
        {
            try
            {
                var syncTokenResult = await GetItemById(connection, id);
                if (syncTokenResult.Status != 200 || syncTokenResult.Data == null)
                {
                    return new CommonResponse<object>(syncTokenResult.Status, "Failed to fetch SyncToken", syncTokenResult.Data);
                }

                var existingCustomer = (QuickBookCustomer)syncTokenResult.Data;
                var payload = new
                {
                    Id = existingCustomer.Id,
                    SyncToken = existingCustomer.SyncToken,
                    DisplayName = existingCustomer.DisplayName,
                    Active = status
                };

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("customer", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksCustomerResponse>(apiResponse.Data.ToString());
                    var unifiedItems = CustomerMapper.MapQuickBooksCustomerToCommon(new List<QuickBookCustomer> { result.Customer });
                    return new CommonResponse<object>(200, "Customer updated successfully", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update Customer", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating Customer", ex.Message);
            }
        }
    
        public class QuickBooksCustomerResponse
        {
            public QuickBookCustomer Customer { get; set; }
        }
    }
}
