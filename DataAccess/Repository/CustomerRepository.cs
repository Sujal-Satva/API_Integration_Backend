using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using task_14.Data;
using task_14.Models;
using task_14.Services;


namespace task_14.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnectionRepository _connectionRepository;

        public CustomerRepository(ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory, IConnectionRepository connectionRepository)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _connectionRepository = connectionRepository;
        }

        public async Task<CommonResponse<object>> FetchAndSaveQBOCustomerAsync()
        {
            return await SyncCustomersAsync("QuickBooks");
        }

        public async Task<CommonResponse<object>> FetchAndSaveXeroCustomerAsync()
        {
            return await SyncCustomersAsync("Xero");
        }

        private async Task<CommonResponse<object>> SyncCustomersAsync(string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                List<AllCustomer> customers;
                if (platform == "QuickBooks")
                {
                    var result = await FetchQuickBooksCustomerAsync(connectionResult.Data);
                    if (result.Status != 200)
                        return new CommonResponse<object>(result.Status, result.Message);
                    customers = result.Data;
                }
                else if (platform == "Xero")
                {
                    var result = await FetchXeroAccountsAsync(connectionResult.Data);
                    if (result.Status != 200)
                        return new CommonResponse<object>(result.Status, result.Message);
                    customers = result.Data;
                }
                else
                {
                    return new CommonResponse<object>(400, $"Unsupported platform: {platform}");
                }

                if (customers == null || !customers.Any())
                    return new CommonResponse<object>(404, $"No accounts found in {platform}.");

                int added = 0, updated = 0;
                foreach (var customer in customers)
                {
                    var existing = await _context.AllCustomers.FirstOrDefaultAsync(c =>
                        c.SourceSystem == customer.SourceSystem &&
                        (
                            (customer.SourceSystem == "QuickBooks" && c.QuickBooksId == customer.QuickBooksId) ||
                            (customer.SourceSystem == "Xero" && c.XeroId == customer.XeroId)
                        ));
                    if (existing == null)
                    {
                        _context.AllCustomers.Add(customer);
                        added++;
                    }
                    else
                    {
                        existing.DisplayName = customer.DisplayName;
                        existing.FirstName = customer.FirstName;
                        existing.LastName = customer.LastName;
                        existing.CompanyName = customer.CompanyName;
                        existing.EmailAddress = customer.EmailAddress;
                        existing.Website = customer.Website;
                        existing.Active = customer.Active;
                        existing.AddressLine1 = customer.AddressLine1;
                        existing.AddressLine2 = customer.AddressLine2;
                        existing.City = customer.City;
                        existing.Region = customer.Region;
                        existing.PostalCode = customer.PostalCode;
                        existing.Country = customer.Country;
                        existing.AddressType = customer.AddressType;
                        existing.PhoneType = customer.PhoneType;
                        existing.PhoneNumber = customer.PhoneNumber;
                        existing.LastUpdatedUtc = customer.LastUpdatedUtc;

                        updated++;
                    }
                }

                await _context.SaveChangesAsync();
                return new CommonResponse<object>(200,
                    $"Customer synced from {platform} successfully. Added: {added}, Updated: {updated}", customers);
            }
            catch (Exception ex)
            {

                return new CommonResponse<object>(500,
                    $"An error occurred while syncing chart of accounts from {platform}");
            }

        }
        private async Task<CommonResponse<List<AllCustomer>>> FetchQuickBooksCustomerAsync(ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var realmId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(realmId))
                    return new CommonResponse<List<AllCustomer>>(401, "Invalid token or realm ID.");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var qbUrl = _configuration["QuickBookBaseUrl"];
                var url = $"{qbUrl}{realmId}/query?query=SELECT * FROM Customer";

                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new CommonResponse<List<AllCustomer>>((int)response.StatusCode,
                        "Error fetching accounts from QuickBooks.");
                }

                var result = JsonConvert.DeserializeObject<CustomerRoot>(json);

                if (result?.QueryResponse?.Customer == null || !result.QueryResponse.Customer.Any())
                {
                    return new CommonResponse<List<AllCustomer>>(404, "No customers found in QuickBooks.", new List<AllCustomer>());
                }

                var customers = result.QueryResponse.Customer
                    .Select(MapQuickBooksCustomerToCommon) 
                    .ToList();

                return new CommonResponse<List<AllCustomer>>(200, "Customers fetched successfully.", customers);
            }
            catch (Exception ex)
            {
                return new CommonResponse<List<AllCustomer>>(500,
                    $"Exception during QuickBooks account synchronization: {ex.Message}");
            }
        }

        private async Task<CommonResponse<List<AllCustomer>>> FetchXeroAccountsAsync(ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var tenantId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tenantId))
                    return new CommonResponse<List<AllCustomer>>(401, "Invalid token or realm ID.");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var xeroUrl = _configuration["Xero:BaseUrl"];
                var url = $"{xeroUrl}/Contacts";
                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new CommonResponse<List<AllCustomer>>((int)response.StatusCode,
                        "Error fetching accounts from Xero.");
                }

                var xeroResult = JsonConvert.DeserializeObject<XeroCustomerResponse>(json);
                var customers = xeroResult.Contacts
                .Select(MapXeroCustomerToCommon)
                .ToList();

                return new CommonResponse<List<AllCustomer>>(200, "Customers fetched successfully.", customers);
            }
            catch (Exception ex)
            {
                return new CommonResponse<List<AllCustomer>>(500,
                    $"Exception during QuickBooks Customers synchronization: {ex.Message}");
            }
        }

        private AllCustomer MapQuickBooksCustomerToCommon(Customer qbCustomer)
        {
            return new AllCustomer
            {
                QuickBooksId = qbCustomer.Id.ToString(),
                SourceSystem = "QuickBooks",
                DisplayName = qbCustomer.DisplayName,
                FirstName = qbCustomer.GivenName,
                LastName = qbCustomer.FamilyName,
                CompanyName = qbCustomer.CompanyName,
                EmailAddress = qbCustomer.PrimaryEmailAddr?.Address,
                Website = null, 
                Active = qbCustomer.Active,
                AddressLine1 = qbCustomer.BillAddr?.Line1,
                AddressLine2 = null, 
                City = qbCustomer.BillAddr?.City,
                Region = qbCustomer.BillAddr?.CountrySubDivisionCode,
                PostalCode = qbCustomer.BillAddr?.PostalCode,
                Country = "USA",
                AddressType = "Billing",
                PhoneType = "Primary",
                PhoneNumber = qbCustomer.PrimaryPhone?.FreeFormNumber,
                LastUpdatedUtc = qbCustomer.MetaData?.LastUpdatedTime?.ToUniversalTime(),
            };
        }

        private AllCustomer MapXeroCustomerToCommon(XeroCustomer xeroCustomer)
        {
          
            var billingAddress = xeroCustomer.Addresses.FirstOrDefault(addr => addr.AddressType == "STREET") ?? xeroCustomer.Addresses.FirstOrDefault();
            var primaryPhone = xeroCustomer.Phones.FirstOrDefault(phone => phone.PhoneType == "DEFAULT");

            return new AllCustomer
            {
                XeroId = xeroCustomer.ContactID,
                SourceSystem = "Xero",
                DisplayName = xeroCustomer.Name,
                FirstName = xeroCustomer.FirstName,
                LastName = xeroCustomer.LastName,
                CompanyName = null,
                EmailAddress = xeroCustomer.EmailAddress,
                Website = xeroCustomer.Website,
                Active = xeroCustomer.ContactStatus == "ACTIVE",
                AddressLine1 = billingAddress?.AddressLine1,
                AddressLine2 = billingAddress?.AddressLine2,
                City = billingAddress?.City,
                Region = billingAddress?.Region,
                PostalCode = billingAddress?.PostalCode,
                Country = billingAddress?.Country,
                AddressType = billingAddress?.AddressType,
                PhoneType = primaryPhone?.PhoneType,
                PhoneNumber = primaryPhone?.PhoneNumber,
                
                LastUpdatedUtc = ParseXeroDate(xeroCustomer.UpdatedDateUTC)
            };
        }

        private DateTime? ParseXeroDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            var match = Regex.Match(dateString, @"\/Date\((\d+)(?:[+-]\d+)?\)\/");

            if (match.Success && long.TryParse(match.Groups[1].Value, out long milliseconds))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
            }

            return null;
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
                var query = _context.AllCustomers
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
                        QuickBooksId=c.QuickBooksId,
                        XeroId=c.XeroId,
                        Active=c.Active,
                        DisplayName = c.DisplayName,
                        FirstName = c.FirstName,
                        AddressLine1=c.AddressLine1,
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


        public async Task<CommonResponse<object>> AddCustomerAsync(CustomerInputModel model, string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                var connection = connectionResult.Data;
                bool addToQbo = false, addToXero = false;

                if (platform == "QuickBooks")
                {
                    addToQbo = await AddCustomerQBOAsync(model, connection);
                }
                else if (platform == "Xero")
                {
                    addToXero = await AddCustomerXeroAsync(model, connection);
                }
         
                if ((platform == "QuickBooks" && addToQbo) ||
                    (platform == "Xero" && addToXero))
                {
                    return new CommonResponse<object>(200, $"Customer added successfully to {platform} and local DB.");
                }

                return new CommonResponse<object>(400, $"Failed to add customer to {platform}.");
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Internal server error", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditCustomerAsync(CustomerInputModel model, string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);
                var connection = connectionResult.Data;
                bool addToQbo = false, addToXero = false;

                if (platform == "QuickBooks")
                {
                    addToQbo = await EditCustomerQBOAsync(model, connection);
                }
                else if (platform == "Xero")
                {
                    addToXero = await EditCustomerXeroAsync(model, connection);
                }

                if ((platform == "QuickBooks" && addToQbo) ||
                    (platform == "Xero" && addToXero))
                {
                    return new CommonResponse<object>(200, $"Customer edited successfully to {platform} and local DB.");
                }

                return new CommonResponse<object>(400, $"Failed to edit customer to {platform}.");
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Internal server error", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> UpdateStatus(string id, string platform, bool status)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                var connection = connectionResult.Data;
                var customer = await _context.AllCustomers.FirstOrDefaultAsync(c =>
                    (platform == "QuickBooks" && c.QuickBooksId == id) ||
                    (platform == "Xero" && c.XeroId == id));

                if (customer == null)
                {
                    return new CommonResponse<object>(404, "Customer not found");
                }
                bool result = false;
                if (platform == "QuickBooks")
                {
                    result = await EditCustomerStatusQBOAsync(id, status, connection);
                }
                else if (platform == "Xero")
                {
                    result = await EditCustomerStatusXeroAsync(id, status, connection);
                }

                if (!result)
                {
                    return new CommonResponse<object>(400, "Failed to update customer status in external system");
                }
                customer.Active = status;
                await _context.SaveChangesAsync();

                return new CommonResponse<object>(200, $"Customer status updated successfully in {platform}");
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Internal server error: " + ex.Message);
            }
        }


        private async Task<bool> EditCustomerStatusXeroAsync(string externalId, bool isActive, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var tenantId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;
                if (string.IsNullOrWhiteSpace(token)) return false;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    Contacts = new[]
                    {
                new
                {
                    ContactID = externalId,
                    IsCustomer = true,
                    ContactStatus = isActive ? "ACTIVE" : "ARCHIVED"
                }
            }
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var xeroUrl = _configuration["Xero:BaseUrl"];
                var url = $"{xeroUrl}/Contacts";

                var response = await httpClient.PostAsync(url, content);
                var contentresponse = await response.Content.ReadAsStringAsync();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }


        private async Task<bool> EditCustomerStatusQBOAsync(string externalId, bool isActive, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var realmId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
                var qburl = _configuration["QuickBookBaseUrl"];

                string getUrl = $"{qburl}{realmId}/customer/{externalId}?minorversion=65";
                string updateUrl = $"{qburl}{realmId}/customer?minorversion=65";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getResponse = await httpClient.GetAsync(getUrl);
                if (!getResponse.IsSuccessStatusCode) return false;

                var getResponseContent = await getResponse.Content.ReadAsStringAsync();
                var quickBooksCustomerResponse = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(getResponseContent);
                var qbCustomer = quickBooksCustomerResponse?.Customer;
                if (qbCustomer == null) return false;

                var payload = new
                {
                    Id = qbCustomer.Id,
                    SyncToken = qbCustomer.SyncToken,
                    DisplayName=qbCustomer.DisplayName,
                    Active = isActive
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var updateResponse = await httpClient.PostAsync(updateUrl, content);

                return updateResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }


        public async Task<CommonResponse<object>>  DeleteCustomerAsync(string id, string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);
                var connection = connectionResult.Data;
                bool addToQbo = false, addToXero = false;

                if (platform == "QuickBooks")
                {
                    addToQbo = await DeleteCustomerQBOAsync(id, connection);
                }
                else if (platform == "Xero")
                {
                    addToXero = await DeleteCustomerXeroAsync(id, connection);
                }

                if ((platform == "QuickBooks" && addToQbo) ||
                    (platform == "Xero" && addToXero))
                {
                    return new CommonResponse<object>(200, $"Customer deleted successfully to {platform} and local DB.");
                }

                return new CommonResponse<object>(400, $"Failed to delete customer to {platform}.");
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Internal server error", ex.Message);
            }
        }

        private async Task<bool> DeleteCustomerQBOAsync(string id, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var realmId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
                var qburl = _configuration["QuickBookBaseUrl"];
                string getUrl = $"{qburl}{realmId}/customer/{id}?minorversion=65";
                string updateUrl = $"{qburl}{realmId}/customer?minorversion=65";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var client = _httpClientFactory.CreateClient();

                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getResponse = await client.SendAsync(getRequest);
                var getJson = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                {
                    return false;
                }
                dynamic existingCustomer = JsonConvert.DeserializeObject(getJson);
                var payload = new
                {
                    Id = id,
                    DisplayName = existingCustomer.Customer.DisplayName,
                    SyncToken = existingCustomer.Customer.SyncToken,
                    Active = false
                };

                var json = JsonConvert.SerializeObject(payload);
                var updateRequest = new HttpRequestMessage(HttpMethod.Post, $"{qburl}{realmId}/customer?operation=update&minorversion=65")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var updateResponse = await client.SendAsync(updateRequest);
                var updateJson = await updateResponse.Content.ReadAsStringAsync();

                if (!updateResponse.IsSuccessStatusCode)
                {
                    return false;
                }
                dynamic qbCustomer = JsonConvert.DeserializeObject(updateJson);
                var localCustomer = await _context.AllCustomers.FirstOrDefaultAsync(c => c.QuickBooksId == id.ToString());
                if (localCustomer != null)
                {
                localCustomer.Active = false;
                    await _context.SaveChangesAsync();
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            } 
        }

        private async Task<bool> DeleteCustomerXeroAsync(string id, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var tenantId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var xeroUrl = _configuration["Xero:BaseUrl"];
                var getUrl = $"{xeroUrl}/Contacts/{id}";
                var getResponse = await client.GetAsync(getUrl);
                var getJson = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                    return false;

                dynamic contactData = JsonConvert.DeserializeObject(getJson);
                if (contactData?.Contacts == null || contactData.Contacts.Count == 0)
                    return false;

                var existingContact = contactData.Contacts[0];
                var payload = new
                {
                    Contacts = new[]
                    {
                new
                {
                    ContactID = id,
                    ContactStatus = "ARCHIVED"
                }
            }
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
               
                var updateUrl = $"{xeroUrl}/Contacts";
                var updateResponse = await client.PostAsync(updateUrl, content);
                var updateJson = await updateResponse.Content.ReadAsStringAsync();

                if (!updateResponse.IsSuccessStatusCode)
                    return false;
                var localCustomer = await _context.AllCustomers.FirstOrDefaultAsync(c => c.XeroId == id);
                if (localCustomer != null)
                {
                    localCustomer.Active = false;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> EditCustomerQBOAsync(CustomerInputModel inputModel, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var realmId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
                var qburl = _configuration["QuickBookBaseUrl"];
                string getUrl = $"{qburl}{realmId}/customer/{inputModel.ExternalId}?minorversion=65";
                string updateUrl = $"{qburl}{realmId}/customer?minorversion=65";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var getResponse = await httpClient.GetAsync(getUrl);
                if (!getResponse.IsSuccessStatusCode) return false;

                var getResponseContent = await getResponse.Content.ReadAsStringAsync();
                var quickBooksCustomerResponse = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(getResponseContent);
                var qbCustomer = quickBooksCustomerResponse?.Customer;

                if (qbCustomer == null) return false;
                var payload = new
                {
                    Id = qbCustomer.Id,
                    SyncToken = qbCustomer.SyncToken,
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

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var updateResponse = await httpClient.PostAsync(updateUrl, content);
                var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();

                if (!updateResponse.IsSuccessStatusCode) return false;

                var updatedCustomer = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(updateResponseContent)?.Customer;
                if (updatedCustomer != null)
                {
                    var existing = await _context.AllCustomers.FirstOrDefaultAsync(x => x.QuickBooksId == (updatedCustomer.Id.ToString()));
                    if (existing != null)
                    {
                        var updatedCommon = MapQuickBooksCustomerToCommon(updatedCustomer);
                        updatedCommon.Id = existing.Id;
                        _context.Entry(existing).CurrentValues.SetValues(updatedCommon);
                        await _context.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> EditCustomerXeroAsync(CustomerInputModel inputModel, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var tenantId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;
                var localCustomer = await _context.AllCustomers
                    .FirstOrDefaultAsync(c => c.XeroId == inputModel.ExternalId && c.SourceSystem == "Xero");
                if (localCustomer == null || string.IsNullOrWhiteSpace(localCustomer.XeroId)) return false;
                var contactId = inputModel.ExternalId;
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var xeroUrl = _configuration["Xero:BaseUrl"];
                var url = $"{xeroUrl}/Contacts";
                var response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                    return false;

                var responseContent = await response.Content.ReadAsStringAsync();
                var xeroResponse = JsonConvert.DeserializeObject<XeroCustomerResponse>(responseContent);
                var updatedContact = xeroResponse?.Contacts?.FirstOrDefault();
                if (updatedContact != null)
                {
                    var updated = MapXeroCustomerToCommon((updatedContact));
                    updated.Id = localCustomer.Id;
                    _context.Entry(localCustomer).CurrentValues.SetValues(updated);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
        private async Task<bool> AddCustomerQBOAsync(CustomerInputModel inputModel, ConnectionModal connection)
        {
            try
            {

                var tokenJson = connection.TokenJson;
                var realmId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
                var qburl = _configuration["QuickBookBaseUrl"];
                string url = $"{qburl}{realmId}/customer?minorversion=65";

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

                var json = JsonConvert.SerializeObject(payload);
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var qbCustomer = JsonConvert.DeserializeObject<QuickBooksAddCustomerResponse>(responseJson);
                var customer=MapQuickBooksCustomerToCommon(qbCustomer.Customer);

                await _context.AllCustomers.AddAsync(customer);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private async Task<bool> AddCustomerXeroAsync(CustomerInputModel inputModel, ConnectionModal connection)
        {
            try
            {
                var tokenJson = connection.TokenJson;
                var tenantId = connection.ExternalId;
                var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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

                var json = JsonConvert.SerializeObject(new { Contacts = new[] { payload } });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var xeroUrl = _configuration["Xero:BaseUrl"];
                var url = $"{xeroUrl}/Contacts";
                var response = await httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var xeroResponse = JsonConvert.DeserializeObject<XeroCustomerResponse>(responseJson);
                var contact = xeroResponse?.Contacts?.FirstOrDefault();

                if (contact != null)
                {
                    var customer = MapXeroCustomerToCommon(contact);
                    await _context.AllCustomers.AddAsync(customer);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //public async Task<ApiResponse<string>> MarkCustomerActiveAsync(string token, string realmId, int customerId)
        //{
        //    try
        //    {
        //        var client = _httpClientFactory.CreateClient();
        //        var qbUrl = _configuration["QuickBookBaseUrl"];
        //        var getUrl = $"{qbUrl}{realmId}/customer/{customerId}?minorversion=65";
        //        var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
        //        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var getResponse = await client.SendAsync(getRequest);
        //        var getJson = await getResponse.Content.ReadAsStringAsync();

        //        if (!getResponse.IsSuccessStatusCode)
        //        {
        //            return new ApiResponse<string>(error: getJson, message: "Failed to fetch customer before activation.");
        //        }
        //        dynamic existingCustomer = JsonConvert.DeserializeObject(getJson);
        //        var payload = new
        //        {
        //            Id = customerId,
        //            DisplayName=existingCustomer.Customer.DisplayName,
        //            SyncToken = existingCustomer.Customer.SyncToken,
        //            Active = true 
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var updateRequest = new HttpRequestMessage(HttpMethod.Post, $"{qbUrl}{realmId}/customer?operation=update&minorversion=65")
        //        {
        //            Content = new StringContent(json, Encoding.UTF8, "application/json")
        //        };
        //        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var updateResponse = await client.SendAsync(updateRequest);
        //        var updateJson = await updateResponse.Content.ReadAsStringAsync();

        //        if (!updateResponse.IsSuccessStatusCode)
        //        {
        //            return new ApiResponse<string>(error: updateJson, message: "Failed to activate customer in QuickBooks.");
        //        }
        //        dynamic qbCustomer = JsonConvert.DeserializeObject(updateJson);
        //        var localCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.QBId == customerId.ToString());
        //        if (localCustomer != null)
        //        {
        //            localCustomer.SyncToken = qbCustomer.Customer.SyncToken;
        //            localCustomer.IsActive = true;
        //            await _context.SaveChangesAsync();
        //        }

        //        return new ApiResponse<string>("Customer activated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<string>(error: ex.Message, message: "Error occurred while activating customer.");
        //    }
        //}
    }
}
