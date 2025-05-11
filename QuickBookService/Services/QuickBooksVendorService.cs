using Newtonsoft.Json;
using QuickBookService.Interfaces;
using QuickBookService.Mappers;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;



namespace QuickBookService.Services
{
    public class QuickBooksVendorService :IQuickBooksVendorService
    {
        private readonly IQuickBooksApiService _quickBooksApiService;

        public QuickBooksVendorService(IQuickBooksApiService quickBooksApiService)
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
        public async Task<CommonResponse<object>> FetchVendorsFromQuickBooks(ConnectionModal connection)
        {
            try
            {
                int startPosition = 1;
                const int maxResults = 1000;
                var lastSync = GetLastSyncedDate(connection.SyncingInfo, "Vendors");

                var allVendors = new List<QuickBooksVendor>();

                while (true)
                {
                    string query = $"SELECT * FROM Vendor WHERE Metadata.LastUpdatedTime > '{lastSync:yyyy-MM-ddTHH:mm:ssZ}' " +
                                   $"STARTPOSITION {startPosition} MAXRESULTS {maxResults}";
                    var apiResponse = await _quickBooksApiService.QuickBooksGetRequest(
                        $"query?query={Uri.EscapeDataString(query)}", connection);

                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch vendors", apiResponse.Data);

                    var result = JsonConvert.DeserializeObject<VendorRoot>(apiResponse.Data.ToString());
                    var vendors = result?.QueryResponse?.Vendor;
                    if (vendors == null || vendors.Count == 0)
                        break;

                    allVendors.AddRange(vendors);
                    if (vendors.Count < maxResults)
                        break;

                    startPosition += maxResults;
                }

                if (allVendors.Count == 0)
                    return new CommonResponse<object>(200, "All vendors are already synced", null);
                var unifiedVendors = VendorMapper.MapToUnifiedVendors(allVendors);
                return new CommonResponse<object>(200, "Vendors fetched and mapped successfully", unifiedVendors);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing vendors", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> AddVendor(ConnectionModal connection, VendorInputModel inputModel)
        {
            try
            {
                var payload = new
                {
                    DisplayName = inputModel.DisplayName,
                    GivenName = inputModel.GivenName ?? "",
                    FamilyName = inputModel.FamilyName ?? "",
                    CompanyName = inputModel.CompanyName ?? "",
                    PrimaryEmailAddr = new { Address = inputModel.EmailAddress ?? "" },
                    PrimaryPhone = new { FreeFormNumber = inputModel.PhoneNumber ?? "" },
                    BillAddr = new
                    {
                        Line1 = inputModel.AddressLine1 ?? "",
                        City = inputModel.City ?? "",
                        CountrySubDivisionCode = inputModel.CountrySubDivisionCode ?? "",
                        PostalCode = inputModel.PostalCode ?? ""
                    },
                    
                };

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("vendor", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = apiResponse.Data;
                    var final = JsonConvert.DeserializeObject<QuickBooksVendorResponse>(apiResponse.Data.ToString());
                    var vendor = final?.Vendor;

                    if (vendor != null)
                    {
                        var vendorList = new List<QuickBooksVendor> { vendor };
                        var unifiedItems = VendorMapper.MapToUnifiedVendors(vendorList);
                        return new CommonResponse<object>(200, "Vendor added successfully", unifiedItems);
                    }

                    return new CommonResponse<object>(200, "Vendor added but could not parse vendor details", null);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to add vendor", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding vendor", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> GetVendorById(ConnectionModal connection, string vendorId)
        {
            try
            {
                var apiResponse = await _quickBooksApiService.QuickBooksGetRequest($"vendor/{vendorId}", connection);

                if (apiResponse.Status == 200)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksVendorResponse>(apiResponse.Data.ToString());
                    return new CommonResponse<object>(200, "Vendor fetched successfully", result.Vendor);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to fetch vendor", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while fetching vendor", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditVendor(ConnectionModal connection, string vendorId, VendorInputModel inputModel)
        {
            try
            {
                var syncTokenResult = await GetVendorById(connection, vendorId);
                if (syncTokenResult.Status != 200 || syncTokenResult.Data == null)
                {
                    return new CommonResponse<object>(syncTokenResult.Status, "Failed to fetch SyncToken", syncTokenResult.Data);
                }

                var existingVendor = (QuickBooksVendor)syncTokenResult.Data;
                var payload = new
                {
                    Id = existingVendor.Id,
                    SyncToken = existingVendor.SyncToken,
                    DisplayName = inputModel.DisplayName,
                    CompanyName = inputModel.CompanyName,
                    PrimaryEmailAddr = new { Address = inputModel.EmailAddress },
                    PrimaryPhone = new { FreeFormNumber = inputModel.PhoneNumber },
                    BillAddr = new
                    {
                        Line1 = inputModel.AddressLine1,
                        City = inputModel.City,
                        CountrySubDivisionCode = inputModel.CountrySubDivisionCode,
                        PostalCode = inputModel.PostalCode
                    },
                    
                };

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("vendor", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksVendorResponse>(apiResponse.Data.ToString());
                    var unifiedItems = VendorMapper.MapToUnifiedVendors(new List<QuickBooksVendor> { result.Vendor });
                    return new CommonResponse<object>(200, "Vendor updated successfully", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update vendor", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating vendor", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> UpdateVendorStatus(ConnectionModal connection, string vendorId, string status)
        {
            try
            {
                var syncTokenResult = await GetVendorById(connection, vendorId);
                if (syncTokenResult.Status != 200 || syncTokenResult.Data == null)
                {
                    return new CommonResponse<object>(syncTokenResult.Status, "Failed to fetch SyncToken", syncTokenResult.Data);
                }

                var existingVendor = (QuickBooksVendor)syncTokenResult.Data;
                var payload = new
                {
                    Id = existingVendor.Id,
                    SyncToken = existingVendor.SyncToken,
                    DisplayName = existingVendor.DisplayName,
                    Active = status
                };

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("vendor", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksVendorResponse>(apiResponse.Data.ToString());
                    var unifiedItems = VendorMapper.MapToUnifiedVendors(new List<QuickBooksVendor> { result.Vendor });
                    return new CommonResponse<object>(200, "Vendor status updated successfully", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update vendor status", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating vendor status", ex.Message);
            }
        }

        public class QuickBooksVendorResponse
        {
            public QuickBooksVendor Vendor
            {
                get; set;
            }
        }
    }
}
