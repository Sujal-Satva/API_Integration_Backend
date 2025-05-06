using Newtonsoft.Json;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using SharedModels.Xero.Models;

using System.Text;
using System.Threading.Tasks;
using XeroService.Interfaces;
using XeroService.Mappers;

namespace XeroService.Services
{
    public class XeroCustomerService:IXeroCustomerService
    {
        private readonly IXeroApiService _xeroApiService;

        public XeroCustomerService(IXeroApiService xeroApiService)
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

        public async Task<CommonResponse<object>> FetchCustomersFromXero(ConnectionModal connection)
        {
            try
            {
                DateTime lastSynced = GetLastSyncedDate(connection.SyncingInfo, "Customers");
                DateTime bufferedTime = lastSynced.ToUniversalTime().AddMinutes(-10);
                string dateFilter = bufferedTime > new DateTime(2000, 1, 1)
                    ? $"?where=UpdatedDateUTC>=DateTime({bufferedTime:yyyy, MM, dd, HH, mm, ss})"
                    : string.Empty;
                int page = 1;
                var allProducts = new List<XeroCustomer>();

                while (true)
                {
                    var apiResponse = await _xeroApiService.XeroGetRequest($"/Contacts{dateFilter}&page={page}", connection);
                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch customers from Xero", apiResponse.Data);
                    var final = JsonConvert.DeserializeObject<XeroCustomerResponse>(apiResponse.Data.ToString());
                    var customers = final?.Contacts;
                    if (customers == null || customers.Count == 0) break;
                    allProducts.AddRange(customers);
                    if (customers.Count < 100)
                        break;
                    page++;
                }
                if (allProducts.Count == 0)
                    return new CommonResponse<object>(200, "All customers are already Synced", null);
                var unifiedItems = CustomerMapper.MapXeroProductsToUnifiedCustomers(allProducts);
                return new CommonResponse<object>(200, "customers fetched and mapped successfully", unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing customers from Xero", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> AddItem(ConnectionModal connection, CustomerInputModel inputModel)
        {
            try
            {
                var payload = new
                {
                    Name = inputModel.DisplayName,
                    FirstName = inputModel.GivenName,
                    LastName = inputModel.FamilyName,
                    EmailAddress = inputModel.EmailAddress,
                    Phones = new[]
                    {
                        new {
                            PhoneType = "DEFAULT",
                            PhoneNumber = inputModel.PhoneNumber
                        }
                    },
                    Addresses = new[]
                    {
                        new {
                            AddressType = "STREET",
                            AddressLine1 = inputModel.AddressLine1,
                            City = inputModel.City,
                            Region = inputModel.CountrySubDivisionCode,
                            PostalCode = inputModel.PostalCode,
                            Country = "USA"
                        }
                    }
                };
                var apiResponse = await _xeroApiService.XeroPostRequest("/Contacts", payload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var final = JsonConvert.DeserializeObject<XeroCustomerResponse>(apiResponse.Data.ToString());
                    var products = final?.Contacts;

                    var unifiedItems = CustomerMapper.MapXeroProductsToUnifiedCustomers(products);

                    return new CommonResponse<object>(200, "Product added successfully to Xero", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to add product to Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding product to Xero", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditCustomer(ConnectionModal connection, string contactId, CustomerInputModel inputModel)
        {
            try
            {
                var payload = new
                {
                    Contacts = new[]
                    {
                        new
                        {
                            ContactID = contactId,
                            Name = inputModel.DisplayName,

                            EmailAddress = inputModel.EmailAddress,
                            Phones = new[]
                            {
                                new
                                {
                                    PhoneType = "DEFAULT",
                                    PhoneNumber = inputModel.PhoneNumber
                                }
                            },
                            Addresses = new[]
                            {
                                new
                                {
                                    AddressType = "STREET",
                                    AddressLine1 = inputModel.AddressLine1,
                                    City = inputModel.City,

                                    PostalCode = inputModel.PostalCode,

                                }
                            }
                        }
                    }
                };
                var apiResponse = await _xeroApiService.XeroPostRequest($"/Contacts/{contactId}", payload, connection);

                if (apiResponse.Status == 200)
                {
                    var final = JsonConvert.DeserializeObject<XeroCustomerResponse>(apiResponse.Data.ToString());
                    var products = final?.Contacts;

                    var unifiedItems = CustomerMapper.MapXeroProductsToUnifiedCustomers(products);
                    return new CommonResponse<object>(200, "Product updated successfully in Xero", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update product in Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating product in Xero", ex.Message);
            }
        }
    }
}
