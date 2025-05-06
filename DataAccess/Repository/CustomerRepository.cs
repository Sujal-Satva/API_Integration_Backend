using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using DataAccess.Helper;
using task_14.Data;
using task_14.Models;
using task_14.Services;
using SharedModels.Models;
using QuickBookService.Interfaces;
using XeroService.Interfaces;
using QuickBookService.Services;
using SharedModels.QuickBooks.Models;
using SharedModels.Xero.Models;


namespace task_14.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuickBooksCustomerService _quickBooksCustomerService;
    
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IXeroCustomerService _xeroCustomerService;
        private readonly SyncingFunction _syncingFunction;
        private readonly IConnectionRepository _connectionRepository;

        public CustomerRepository(ApplicationDbContext context, SyncingFunction syncingFunction, IConfiguration configuration, IHttpClientFactory httpClientFactory, IConnectionRepository connectionRepository,IQuickBooksCustomerService quickBooksCustomerService,IXeroCustomerService xeroCustomerService)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _connectionRepository = connectionRepository;
            _syncingFunction = syncingFunction;
            _quickBooksCustomerService = quickBooksCustomerService;
            _xeroCustomerService = xeroCustomerService;
        }

        

        public async Task<CommonResponse<object>> SyncCustomers(string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);
                CommonResponse<object> response = null;

                if (platform == "QuickBooks")
                {
                    response = await _quickBooksCustomerService.FetchCustomersFromQuickBooks(connectionResult.Data);
                }
                else
                {
                    response = await _xeroCustomerService.FetchCustomersFromXero(connectionResult.Data);
                }
                if (response.Data == null)
                    return new CommonResponse<object>(response.Status, response.Message);
                var unifiedCustomers = response.Data as List<UnifiedCustomer>;
                await _syncingFunction.UpdateSyncingInfo(connectionResult.Data.ExternalId, "Customers", DateTime.UtcNow);
                return await _syncingFunction.StoreUnifiedCustomersAsync(unifiedCustomers);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Failed to insert items", ex.Message);
            }
        }

        public async Task<CommonResponse<PagedResponse<CustomerDTO>>> GetCustomersFromDbAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortColumn = "DisplayName",
            string? sortDirection = "asc",
            string? sourceSystem = null,
            bool active = true,
            bool pagination = true)
        {
            try
            {
                var query = _context.UnifiedCustomers
                    .Where(c => c.Active == active);
                if (!string.IsNullOrWhiteSpace(sourceSystem))
                    query = query.Where(c => c.SourceSystem.ToLower() == sourceSystem.ToLower());
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c =>
                        c.DisplayName.Contains(search) ||
                        c.FirstName.Contains(search) ||
                        c.LastName.Contains(search) ||
                        c.EmailAddress.Contains(search) ||
                        c.PhoneNumber.Contains(search) ||
                        c.AddressLine1.Contains(search) ||
                        c.AddressLine2.Contains(search) ||
                        c.City.Contains(search)
                    );
                }
                switch (sortColumn?.ToLower())
                {
                    case "emailAddress":
                        query = sortDirection.ToLower() == "desc"
                            ? query.OrderByDescending(c => c.EmailAddress)
                            : query.OrderBy(c => c.EmailAddress);
                        break;
                    case "city":
                        query = sortDirection.ToLower() == "desc"
                            ? query.OrderByDescending(c => c.City)
                            : query.OrderBy(c => c.City);
                        break;

                    case "lastupdatedutc":
                        query = sortDirection.ToLower() == "desc"
                            ? query.OrderByDescending(c => c.LastUpdatedUtc)
                            : query.OrderBy(c => c.LastUpdatedUtc);
                        break;

                    case "displayname":
                    default:
                        query = sortDirection.ToLower() == "desc"
                            ? query.OrderByDescending(c => c.DisplayName)
                            : query.OrderBy(c => c.DisplayName);
                        break;
                }

                var totalRecords = await query.CountAsync();
                if (pagination)
                {
                    query = query.Skip((page - 1) * pageSize).Take(pageSize);
                }
                var customers = await query
                    .Select(c => new CustomerDTO
                    {
                        Id = c.Id,
                        ExternalId=c.ExternalId,
                        Active = c.Active,
                        DisplayName = c.DisplayName,
                        FirstName = c.FirstName,
                        AddressLine1 = c.AddressLine1,
                        LastName = c.LastName,
                        EmailAddress = c.EmailAddress,
                        PhoneNumber = c.PhoneNumber,
                        City = c.City,
                        SourceSystem = c.SourceSystem,
                        LastUpdatedUtc = c.LastUpdatedUtc
                    })
                    .ToListAsync();
                var pagedResult = new PagedResponse<CustomerDTO>(customers, pagination ? page : 1, pagination ? pageSize : totalRecords, totalRecords);
                return new CommonResponse<PagedResponse<CustomerDTO>>(200, "Customers fetched successfully", pagedResult);
            }
            catch (Exception ex)
            {
                return new CommonResponse<PagedResponse<CustomerDTO>>(500, $"Error fetching customers: {ex.Message}", null);
            }
        }


        public async Task<CommonResponse<object>> AddCustomersAsync(string platform, object input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(platform))
                    return new CommonResponse<object>(400, "Platform is required");

                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                CommonResponse<object> response;

                switch (platform.ToLower())
                {
                    case "xero":
                        var xeroModel = JsonConvert.DeserializeObject<CustomerInputModel>(input.ToString());
                        response = await _xeroCustomerService.AddItem(connectionResult.Data, xeroModel);
                        break;

                    case "quickbooks":
                        var qbModel = JsonConvert.DeserializeObject<CustomerInputModel>(input.ToString());
                        response = await _quickBooksCustomerService.AddItem(connectionResult.Data, qbModel);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedItem> items && items.Any())
                {
                    await _syncingFunction.StoreUnifiedItemsAsync(items);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while adding the item", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> EditCustomersAsync(string platform, string itemId, CustomerInputModel input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(platform))
                    return new CommonResponse<object>(400, "Platform is required");

                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                CommonResponse<object> response;

                switch (platform.ToLower())
                {
                    case "xero":
                        response = await _xeroCustomerService.EditCustomer(connectionResult.Data, itemId, input);
                        break;

                    case "quickbooks":
                        response = await _quickBooksCustomerService.EditCustomer(connectionResult.Data, itemId, input);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedCustomer> customers && customers.FirstOrDefault() is UnifiedCustomer updatedCustomer)
                {
                    await _syncingFunction.StoreUnifiedCustomersAsync(customers);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while editing the item", ex.Message);
            }
        }


        //public async Task<CommonResponse<object>> AddCustomerAsync(CustomerInputModel model, string platform)
        //{
        //    try
        //    {
        //        var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
        //        if (connectionResult.Status != 200 || connectionResult.Data == null)
        //            return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

        //        var connection = connectionResult.Data;
        //        bool addToQbo = false, addToXero = false;

        //        if (platform == "QuickBooks")
        //        {
        //            addToQbo = await AddCustomerQBOAsync(model, connection);
        //        }
        //        else if (platform == "Xero")
        //        {
        //            addToXero = await AddCustomerXeroAsync(model, connection);
        //        }

        //        if ((platform == "QuickBooks" && addToQbo) ||
        //            (platform == "Xero" && addToXero))
        //        {
        //            return new CommonResponse<object>(200, $"Customer added successfully to {platform} and local DB.");
        //        }

        //        return new CommonResponse<object>(400, $"Failed to add customer to {platform}.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponse<object>(500, "Internal server error", ex.Message);
        //    }
        //}

        //public async Task<CommonResponse<object>> EditCustomerAsync(CustomerInputModel model, string platform)
        //{
        //    try
        //    {
        //        var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
        //        if (connectionResult.Status != 200 || connectionResult.Data == null)
        //            return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);
        //        var connection = connectionResult.Data;
        //        bool addToQbo = false, addToXero = false;

        //        if (platform == "QuickBooks")
        //        {
        //            addToQbo = await EditCustomerQBOAsync(model, connection);
        //        }
        //        else if (platform == "Xero")
        //        {
        //            addToXero = await EditCustomerXeroAsync(model, connection);
        //        }

        //        if ((platform == "QuickBooks" && addToQbo) ||
        //            (platform == "Xero" && addToXero))
        //        {
        //            return new CommonResponse<object>(200, $"Customer edited successfully to {platform} and local DB.");
        //        }

        //        return new CommonResponse<object>(400, $"Failed to edit customer to {platform}.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponse<object>(500, "Internal server error", ex.Message);
        //    }
        //}

        //public async Task<CommonResponse<object>> UpdateStatus(string id, string platform, bool status)
        //{
        //    try
        //    {
        //        var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
        //        if (connectionResult.Status != 200 || connectionResult.Data == null)
        //            return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

        //        var connection = connectionResult.Data;
        //        var customer = await _context.AllCustomers.FirstOrDefaultAsync(c =>
        //            (platform == "QuickBooks" && c.QuickBooksId == id) ||
        //            (platform == "Xero" && c.XeroId == id));

        //        if (customer == null)
        //        {
        //            return new CommonResponse<object>(404, "Customer not found");
        //        }
        //        bool result = false;
        //        if (platform == "QuickBooks")
        //        {
        //            result = await EditCustomerStatusQBOAsync(id, status, connection);
        //        }
        //        else if (platform == "Xero")
        //        {
        //            result = await EditCustomerStatusXeroAsync(id, status, connection);
        //        }

        //        if (!result)
        //        {
        //            return new CommonResponse<object>(400, "Failed to update customer status in external system");
        //        }
        //        customer.Active = status;
        //        await _context.SaveChangesAsync();

        //        return new CommonResponse<object>(200, $"Customer status updated successfully in {platform}");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponse<object>(500, "Internal server error: " + ex.Message);
        //    }
        //}


        //private async Task<bool> EditCustomerStatusXeroAsync(string externalId, bool isActive, ConnectionModal connection)
        //{
        //    try
        //    {
        //        var tokenJson = connection.TokenJson;
        //        var tenantId = connection.ExternalId;
        //        var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;
        //        if (string.IsNullOrWhiteSpace(token)) return false;

        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
        //        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var payload = new
        //        {
        //            Contacts = new[]
        //            {
        //        new
        //        {
        //            ContactID = externalId,
        //            IsCustomer = true,
        //            ContactStatus = isActive ? "ACTIVE" : "ARCHIVED"
        //        }
        //    }
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //        var xeroUrl = _configuration["Xero:BaseUrl"];
        //        var url = $"{xeroUrl}/Contacts";

        //        var response = await httpClient.PostAsync(url, content);
        //        var contentresponse = await response.Content.ReadAsStringAsync();
        //        return response.IsSuccessStatusCode;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}


        //private async Task<bool> EditCustomerStatusQBOAsync(string externalId, bool isActive, ConnectionModal connection)
        //{
        //    try
        //    {
        //        var tokenJson = connection.TokenJson;
        //        var realmId = connection.ExternalId;
        //        var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
        //        var qburl = _configuration["QuickBookBaseUrl"];

        //        string getUrl = $"{qburl}{realmId}/customer/{externalId}?minorversion=65";
        //        string updateUrl = $"{qburl}{realmId}/customer?minorversion=65";

        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var getResponse = await httpClient.GetAsync(getUrl);
        //        if (!getResponse.IsSuccessStatusCode) return false;

        //        var getResponseContent = await getResponse.Content.ReadAsStringAsync();
        //        var quickBooksCustomerResponse = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(getResponseContent);
        //        var qbCustomer = quickBooksCustomerResponse?.Customer;
        //        if (qbCustomer == null) return false;

        //        var payload = new
        //        {
        //            Id = qbCustomer.Id,
        //            SyncToken = qbCustomer.SyncToken,
        //            DisplayName=qbCustomer.DisplayName,
        //            Active = isActive
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //        var updateResponse = await httpClient.PostAsync(updateUrl, content);

        //        return updateResponse.IsSuccessStatusCode;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private async Task<bool> EditCustomerQBOAsync(CustomerInputModel inputModel, ConnectionModal connection)
        //{
        //    try
        //    {
        //        var tokenJson = connection.TokenJson;
        //        var realmId = connection.ExternalId;
        //        var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
        //        var qburl = _configuration["QuickBookBaseUrl"];
        //        string getUrl = $"{qburl}{realmId}/customer/{inputModel.ExternalId}?minorversion=65";
        //        string updateUrl = $"{qburl}{realmId}/customer?minorversion=65";

        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //        var getResponse = await httpClient.GetAsync(getUrl);
        //        if (!getResponse.IsSuccessStatusCode) return false;

        //        var getResponseContent = await getResponse.Content.ReadAsStringAsync();
        //        var quickBooksCustomerResponse = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(getResponseContent);
        //        var qbCustomer = quickBooksCustomerResponse?.Customer;

        //        if (qbCustomer == null) return false;
        //        var payload = new
        //        {
        //            Id = qbCustomer.Id,
        //            SyncToken = qbCustomer.SyncToken,
        //            DisplayName = inputModel.DisplayName,
        //            PrimaryEmailAddr = new { Address = inputModel.EmailAddress },
        //            PrimaryPhone = new { FreeFormNumber = inputModel.PhoneNumber },
        //            BillAddr = new
        //            {
        //                Line1 = inputModel.AddressLine1,
        //                City = inputModel.City,
        //                CountrySubDivisionCode = inputModel.CountrySubDivisionCode,
        //                PostalCode = inputModel.PostalCode,
        //            }
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //        var updateResponse = await httpClient.PostAsync(updateUrl, content);
        //        var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();

        //        if (!updateResponse.IsSuccessStatusCode) return false;

        //        var updatedCustomer = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(updateResponseContent)?.Customer;
        //        if (updatedCustomer != null)
        //        {
        //            var existing = await _context.AllCustomers.FirstOrDefaultAsync(x => x.QuickBooksId == (updatedCustomer.Id.ToString()));
        //            if (existing != null)
        //            {
        //                var updatedCommon = MapQuickBooksCustomerToCommon(updatedCustomer);
        //                updatedCommon.Id = existing.Id;
        //                _context.Entry(existing).CurrentValues.SetValues(updatedCommon);
        //                await _context.SaveChangesAsync();
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public async Task<bool> EditCustomerXeroAsync(CustomerInputModel inputModel, ConnectionModal connection)
        //{
        //    try
        //    {
        //        var tokenJson = connection.TokenJson;
        //        var tenantId = connection.ExternalId;
        //        var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;
        //        var localCustomer = await _context.AllCustomers
        //            .FirstOrDefaultAsync(c => c.XeroId == inputModel.ExternalId && c.SourceSystem == "Xero");
        //        if (localCustomer == null || string.IsNullOrWhiteSpace(localCustomer.XeroId)) return false;
        //        var contactId = inputModel.ExternalId;
        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
        //        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var payload = new
        //        {
        //            Contacts = new[]
        //            {
        //        new
        //        {
        //            ContactID = contactId,
        //            Name = inputModel.DisplayName,

        //            EmailAddress = inputModel.EmailAddress,
        //            Phones = new[]
        //            {
        //                new
        //                {
        //                    PhoneType = "DEFAULT",
        //                    PhoneNumber = inputModel.PhoneNumber
        //                }
        //            },
        //            Addresses = new[]
        //            {
        //                new
        //                {
        //                    AddressType = "STREET",
        //                    AddressLine1 = inputModel.AddressLine1,
        //                    City = inputModel.City,

        //                    PostalCode = inputModel.PostalCode,

        //                }
        //            }
        //        }
        //    }
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //        var xeroUrl = _configuration["Xero:BaseUrl"];
        //        var url = $"{xeroUrl}/Contacts";
        //        var response = await httpClient.PostAsync(url, content);

        //        if (!response.IsSuccessStatusCode)
        //            return false;

        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        var xeroResponse = JsonConvert.DeserializeObject<XeroCustomerResponse>(responseContent);
        //        var updatedContact = xeroResponse?.Contacts?.FirstOrDefault();
        //        if (updatedContact != null)
        //        {
        //            var updated = MapXeroCustomerToCommon((updatedContact));
        //            updated.Id = localCustomer.Id;
        //            _context.Entry(localCustomer).CurrentValues.SetValues(updated);
        //            await _context.SaveChangesAsync();
        //            return true;
        //        }

        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        //private async Task<bool> AddCustomerQBOAsync(CustomerInputModel inputModel, ConnectionModal connection)
        //{
        //    try
        //    {

        //        var tokenJson = connection.TokenJson;
        //        var realmId = connection.ExternalId;
        //        var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
        //        var qburl = _configuration["QuickBookBaseUrl"];
        //        string url = $"{qburl}{realmId}/customer?minorversion=65";

        //        var payload = new
        //        {
        //            DisplayName = inputModel.DisplayName,
        //            GivenName = inputModel.GivenName ?? "",
        //            FamilyName = inputModel.FamilyName ?? "",
        //            PrimaryEmailAddr = new { Address = inputModel.EmailAddress ?? "" },
        //            PrimaryPhone = new { FreeFormNumber = inputModel.PhoneNumber ?? "" },
        //            BillAddr = new
        //            {
        //                Line1 = inputModel.AddressLine1 ?? "",
        //                City = inputModel.City ?? "",
        //                CountrySubDivisionCode = inputModel.CountrySubDivisionCode ?? "",
        //                PostalCode = inputModel.PostalCode ?? ""
        //            }
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var client = _httpClientFactory.CreateClient();
        //        var request = new HttpRequestMessage(HttpMethod.Post, url)
        //        {
        //            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        //        };
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var response = await client.SendAsync(request);
        //        var responseJson = await response.Content.ReadAsStringAsync();

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return false;
        //        }

        //        var qbCustomer = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(responseJson);
        //        var customer=MapQuickBooksCustomerToCommon(qbCustomer.Customer);

        //        await _context.AllCustomers.AddAsync(customer);
        //        await _context.SaveChangesAsync();

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
        //private async Task<bool> AddCustomerXeroAsync(CustomerInputModel inputModel, ConnectionModal connection)
        //{
        //    try
        //    {
        //        var tokenJson = connection.TokenJson;
        //        var tenantId = connection.ExternalId;
        //        var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;

        //        using var httpClient = new HttpClient();
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
        //        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var payload = new
        //        {
        //            Name = inputModel.DisplayName,
        //            FirstName = inputModel.GivenName,
        //            LastName = inputModel.FamilyName,
        //            EmailAddress = inputModel.EmailAddress,
        //            Phones = new[]
        //            {
        //        new {
        //            PhoneType = "DEFAULT",
        //            PhoneNumber = inputModel.PhoneNumber
        //        }
        //    },
        //            Addresses = new[]
        //            {
        //        new {
        //            AddressType = "STREET",
        //            AddressLine1 = inputModel.AddressLine1,
        //            City = inputModel.City,
        //            Region = inputModel.CountrySubDivisionCode,
        //            PostalCode = inputModel.PostalCode,
        //            Country = "USA"
        //        }
        //    }
        //        };

        //        var json = JsonConvert.SerializeObject(new { Contacts = new[] { payload } });
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //        var xeroUrl = _configuration["Xero:BaseUrl"];
        //        var url = $"{xeroUrl}/Contacts";
        //        var response = await httpClient.PostAsync(url, content);
        //        var responseJson = await response.Content.ReadAsStringAsync();

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return false;
        //        }

        //        var xeroResponse = JsonConvert.DeserializeObject<XeroCustomerResponse>(responseJson);
        //        var contact = xeroResponse?.Contacts?.FirstOrDefault();

        //        if (contact != null)
        //        {
        //            var customer = MapXeroCustomerToCommon(contact);
        //            await _context.AllCustomers.AddAsync(customer);
        //            await _context.SaveChangesAsync();
        //            return true;
        //        }

        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}
