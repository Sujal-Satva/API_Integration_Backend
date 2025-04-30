using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using task_14.Data;
using task_14.Models;
using task_14.Services;

namespace task_14.Repository
{
    public class VendorRepository:IVendorRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public VendorRepository(ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Vendor>> SyncVendorsFromQuickBooksAsync(string token, string realmId)
        {
            var vendorList = new List<Vendor>();

            try
            {
                var lastSyncTime = _context.Vendors
                    .OrderByDescending(v => v.LastUpdatedTime)
                    .FirstOrDefault()?.LastUpdatedTime ?? DateTime.MinValue;
                var qburl = _configuration["QuickBookBaseUrl"];
                var url = $"{qburl}{realmId}/query?query=SELECT * FROM Vendor where Active IN (true,false)";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch vendors from QuickBooks. Status code: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                var vendors = data?.QueryResponse?.Vendor;

                if (vendors == null || vendors.Count == 0)
                {
                    return vendorList;
                }

                foreach (var vendor in vendors)
                {
                    var lastUpdatedTime = DateTime.Parse(vendor.MetaData?.LastUpdatedTime.ToString());

                    if (lastUpdatedTime > lastSyncTime)
                    {
                        var vendorObj = new Vendor
                        {
                            VId = vendor.Id ?? "0",
                            DisplayName = vendor.DisplayName ?? "",
                            Active = vendor.Active ?? false,
                            Vendor1099 = vendor.Vendor1099 ?? false,
                            Balance = vendor.Balance ?? 0m,
                            CurrencyValue = vendor.CurrencyRef?.value ?? "",
                            CurrencyName = vendor.CurrencyRef?.name ?? "",
                            BillAddrLine1 = vendor.BillAddr?.Line1 ?? "",
                            BillAddrCity = vendor.BillAddr?.City ?? "",
                            BillAddrPostalCode = vendor.BillAddr?.PostalCode ?? "",
                            SyncToken = vendor.SyncToken ?? "",
                            V4IDPseudonym = vendor.V4IDPseudonym ?? "",
                            PrimaryPhone = vendor.PrimaryPhone?.FreeFormNumber ?? "",
                            PrimaryEmailAddr = vendor.PrimaryEmailAddr?.Address ?? "",
                            WebAddr = vendor.WebAddr?.URI ?? "",
                            CreateTime = vendor.MetaData?.CreateTime != null
                                         ? DateTime.Parse(vendor.MetaData?.CreateTime.ToString())
                                         : DateTime.MinValue,
                            LastUpdatedTime = lastUpdatedTime,
                            GivenName = vendor.GivenName ?? "",
                            FamilyName = vendor.FamilyName ?? "",
                            CompanyName = vendor.CompanyName ?? ""
                        };

                      
                        var existingVendor = await _context.Vendors
                            .FirstOrDefaultAsync(v => v.VId == vendorObj.VId);

                        if (existingVendor == null)
                        {
                            _context.Vendors.Add(vendorObj);
                        }
                        else
                        {
                            existingVendor.DisplayName = vendorObj.DisplayName;
                            existingVendor.Active = vendorObj.Active;
                            existingVendor.Vendor1099 = vendorObj.Vendor1099;
                            existingVendor.Balance = vendorObj.Balance;
                            existingVendor.CurrencyValue = vendorObj.CurrencyValue;
                            existingVendor.CurrencyName = vendorObj.CurrencyName;
                            existingVendor.BillAddrLine1 = vendorObj.BillAddrLine1;
                            existingVendor.BillAddrCity = vendorObj.BillAddrCity;
                            existingVendor.BillAddrPostalCode = vendorObj.BillAddrPostalCode;
                            existingVendor.SyncToken = vendorObj.SyncToken;
                            existingVendor.V4IDPseudonym = vendorObj.V4IDPseudonym;
                            existingVendor.PrimaryPhone = vendorObj.PrimaryPhone;
                            existingVendor.PrimaryEmailAddr = vendorObj.PrimaryEmailAddr;
                            existingVendor.WebAddr = vendorObj.WebAddr;
                            existingVendor.CreateTime = vendorObj.CreateTime;
                            existingVendor.LastUpdatedTime = vendorObj.LastUpdatedTime;
                            existingVendor.GivenName = vendorObj.GivenName;
                            existingVendor.FamilyName = vendorObj.FamilyName;
                            existingVendor.CompanyName = vendorObj.CompanyName;
                        }


                        vendorList.Add(vendorObj);
                    }
                }

                if (vendorList.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return vendorList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error syncing vendors: {ex.Message}");
            }
        }


        public async Task<PagedResponse<Vendor>> GetAllActiveVendorsAsync(string? search, string? sortColumn, string? sortDirection, bool pagination, int page, int pageSize,bool active)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            IQueryable<Vendor> query;

            if (active)
            {
                query = _context.Vendors.Where(v => v.Active).AsQueryable();
            }
            else
            {
                query = _context.Vendors.Where(v => v.Active == false).AsQueryable();
            }
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v =>
                    v.DisplayName.Contains(search) ||
                    v.PrimaryPhone.Contains(search) ||
                    v.PrimaryEmailAddr.Contains(search) ||
                    v.WebAddr.Contains(search) ||
                    v.BillAddrLine1.Contains(search) ||
                    v.BillAddrCity.Contains(search) ||
                    v.BillAddrPostalCode.Contains(search) ||
                    v.CompanyName.Contains(search) ||
                    v.GivenName.Contains(search) ||
                    v.FamilyName.Contains(search));
            }

            sortColumn ??= "DisplayName";
            sortDirection ??= "asc";

            switch (sortColumn.ToLower())
            {
                case "displayname":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.DisplayName) : query.OrderBy(v => v.DisplayName);
                    break;
                case "balance":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.Balance) : query.OrderBy(v => v.Balance);
                    break;
                case "currencyvalue":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.CurrencyValue) : query.OrderBy(v => v.CurrencyValue);
                    break;
                case "currencyname":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.CurrencyName) : query.OrderBy(v => v.CurrencyName);
                    break;
                case "createdtime":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.CreateTime) : query.OrderBy(v => v.CreateTime);
                    break;
                case "lastupdatedtime":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.LastUpdatedTime) : query.OrderBy(v => v.LastUpdatedTime);
                    break;
                case "givenname":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.GivenName) : query.OrderBy(v => v.GivenName);
                    break;
                case "familyname":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.FamilyName) : query.OrderBy(v => v.FamilyName);
                    break;
                case "companyname":
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.CompanyName) : query.OrderBy(v => v.CompanyName);
                    break;
                default:
                    query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(v => v.DisplayName) : query.OrderBy(v => v.DisplayName);
                    break;
            }

            if (!pagination)
            {
                var result = await query.ToListAsync();
                return new PagedResponse<Vendor>(result, page, result.Count, result.Count);
            }

            var totalRecords = await query.CountAsync();
            var pagedVendors = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResponse<Vendor>(pagedVendors, page, pageSize, totalRecords);
        }

        public async Task<ApiResponse<string>> AddVendorAsync(VendorInputModal inputModel, string token, string realmId)
        {
            try
            {
                var qburl = _configuration["QuickBookBaseUrl"];
                string url = $"{qburl}{realmId}/vendor?minorversion=65";

                var payload = new
                {
                    DisplayName = inputModel.DisplayName,
                    GivenName = inputModel.GivenName,
                    FamilyName = inputModel.FamilyName,
                    CompanyName = inputModel.CompanyName,
                    Active = inputModel.Active,
                    Balance = inputModel.Balance,
                    CurrencyRef = new { value = inputModel.CurrencyValue, name = inputModel.CurrencyName },
                    BillAddr = new
                    {
                        Line1 = inputModel.BillAddrLine1,
                        City = inputModel.BillAddrCity,
                        CountrySubDivisionCode = inputModel.BillAddrPostalCode,
                        PostalCode = inputModel.BillAddrPostalCode
                    },
                    PrimaryPhone = new { FreeFormNumber = inputModel.PrimaryPhone },
                    PrimaryEmailAddr = new { Address = inputModel.PrimaryEmailAddr },
                    WebAddr = new { URI = inputModel.WebAddr }
                };

                var json = JsonConvert.SerializeObject(payload);
                var client = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResponse<string>(error: responseJson, message: "Failed to create vendor in QuickBooks");
                }

                dynamic qbVendor = JsonConvert.DeserializeObject(responseJson);

                var createdVendor = new Vendor
                {
                    VId = qbVendor.Vendor.Id,
                    SyncToken = qbVendor.Vendor.SyncToken,
                    DisplayName = qbVendor.Vendor.DisplayName,
                    GivenName = qbVendor.Vendor.GivenName ?? "",
                    FamilyName = qbVendor.Vendor.FamilyName ?? "",
                    CompanyName = qbVendor.Vendor.CompanyName ?? "",
                    Active = qbVendor.Vendor.Active ?? true,
                    Vendor1099 = qbVendor.Vendor.Vendor1099 ?? false,
                    Balance = qbVendor.Vendor.Balance ?? 0,
                    V4IDPseudonym = qbVendor.Vendor.V4IDPseudonym ?? "",
                    CurrencyValue = qbVendor.Vendor.CurrencyRef?.value ?? "",
                    CurrencyName = qbVendor.Vendor.CurrencyRef?.name ?? "",
                    BillAddrLine1 = qbVendor.Vendor.BillAddr?.Line1 ?? "",
                    BillAddrCity = qbVendor.Vendor.BillAddr?.City ?? "",
                    BillAddrPostalCode = qbVendor.Vendor.BillAddr?.CountrySubDivisionCode ?? "",
                    PrimaryPhone = qbVendor.Vendor.PrimaryPhone?.FreeFormNumber ?? "",
                    PrimaryEmailAddr = qbVendor.Vendor.PrimaryEmailAddr?.Address ?? "",
                    WebAddr = qbVendor.Vendor.WebAddr?.URI ?? "",
                    CreateTime = qbVendor.Vendor.MetaData?.CreateTime != null
                        ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.CreateTime.ToString()).UtcDateTime
                        : (DateTime?)null,
                    LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
                        ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
                        : (DateTime?)null
                };

                await _context.Vendors.AddAsync(createdVendor);
                await _context.SaveChangesAsync();

                return new ApiResponse<string>("Vendor added successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(error: ex.Message, message: "Error occurred while adding vendor.");
            }
        }

        public async Task<ApiResponse<string>> UpdateVendorAsync(string vendorId, VendorInputModal inputModel, string token, string realmId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var qburl = _configuration["QuickBookBaseUrl"];
                var getUrl = $"{qburl}{realmId}/vendor/{vendorId}?minorversion=65";
                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getResponse = await client.SendAsync(getRequest);
                var getJson = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<string>(error: getJson, message: "Failed to fetch vendor before update.");
                }

                dynamic existingVendor = JsonConvert.DeserializeObject(getJson);

                // Step 2: Prepare update payload
                var payload = new
                {
                    Id = vendorId,
                    SyncToken = existingVendor.Vendor.SyncToken,
                    DisplayName = inputModel.DisplayName,
                    GivenName = inputModel.GivenName,
                    FamilyName = inputModel.FamilyName,
                    CompanyName = inputModel.CompanyName,
                    Active = inputModel.Active,
                    Balance = inputModel.Balance,
                    CurrencyRef = new { value = inputModel.CurrencyValue, name = inputModel.CurrencyName },
                    BillAddr = new
                    {
                        Line1 = inputModel.BillAddrLine1,
                        City = inputModel.BillAddrCity,
                        CountrySubDivisionCode = inputModel.BillAddrPostalCode,
                        PostalCode = inputModel.BillAddrPostalCode
                    },
                    PrimaryPhone = new { FreeFormNumber = inputModel.PrimaryPhone },
                    PrimaryEmailAddr = new { Address = inputModel.PrimaryEmailAddr },
                    WebAddr = new { URI = inputModel.WebAddr }
                };

                var json = JsonConvert.SerializeObject(payload);
                var updateRequest = new HttpRequestMessage(HttpMethod.Post, $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/vendor?minorversion=65")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var updateResponse = await client.SendAsync(updateRequest);
                var updateJson = await updateResponse.Content.ReadAsStringAsync();

                if (!updateResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<string>(error: updateJson, message: "Failed to update vendor in QuickBooks.");
                }

                dynamic qbVendor = JsonConvert.DeserializeObject(updateJson);

                // Update local DB
                var localVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VId == vendorId);
                if (localVendor != null)
                {
                    localVendor.DisplayName = qbVendor.Vendor.DisplayName;
                    localVendor.GivenName = qbVendor.Vendor.GivenName ?? "";
                    localVendor.FamilyName = qbVendor.Vendor.FamilyName ?? "";
                    localVendor.CompanyName = qbVendor.Vendor.CompanyName ?? "";
                    localVendor.Active = qbVendor.Vendor.Active ?? true;
                    localVendor.Vendor1099 = qbVendor.Vendor.Vendor1099 ?? false;
                    localVendor.Balance = qbVendor.Vendor.Balance ?? 0;
                    localVendor.V4IDPseudonym = qbVendor.Vendor.V4IDPseudonym ?? "";
                    localVendor.CurrencyValue = qbVendor.Vendor.CurrencyRef?.value ?? "";
                    localVendor.CurrencyName = qbVendor.Vendor.CurrencyRef?.name ?? "";
                    localVendor.BillAddrLine1 = qbVendor.Vendor.BillAddr?.Line1 ?? "";
                    localVendor.BillAddrCity = qbVendor.Vendor.BillAddr?.City ?? "";
                    localVendor.BillAddrPostalCode = qbVendor.Vendor.BillAddr?.CountrySubDivisionCode ?? "";
                    localVendor.PrimaryPhone = qbVendor.Vendor.PrimaryPhone?.FreeFormNumber ?? "";
                    localVendor.PrimaryEmailAddr = qbVendor.Vendor.PrimaryEmailAddr?.Address ?? "";
                    localVendor.WebAddr = qbVendor.Vendor.WebAddr?.URI ?? "";
                    localVendor.LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
                        ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
                        : (DateTime?)null;

                    await _context.SaveChangesAsync();
                }

                return new ApiResponse<string>("Vendor updated successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(error: ex.Message, message: "Error occurred while updating vendor.");
            }
        }

        public async Task<ApiResponse<string>> DeactivateVendorAsync(string vendorId, string token, string realmId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var qburl = _configuration["QuickBookBaseUrl"];
                var getUrl = $"{qburl}{realmId}/vendor/{vendorId}?minorversion=65";
                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getResponse = await client.SendAsync(getRequest);
                var getJson = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                    return new ApiResponse<string>(error: getJson, message: "Failed to fetch vendor before delete.");

                dynamic existingVendor = JsonConvert.DeserializeObject(getJson);
                var payload = new
                {
                    DisplayName = existingVendor.Vendor.DisplayName,
                    Id = vendorId,
                    SyncToken = existingVendor.Vendor.SyncToken,
                    Active = false
                };

                var json = JsonConvert.SerializeObject(payload);
                var deleteRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{qburl}{realmId}/vendor?operation=update&minorversion=65")
                {
                    Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
                };
                deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                deleteRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var deleteResponse = await client.SendAsync(deleteRequest);
                var deleteJson = await deleteResponse.Content.ReadAsStringAsync();

                if (!deleteResponse.IsSuccessStatusCode)
                    return new ApiResponse<string>(error: deleteJson, message: "Failed to deactivate vendor in QuickBooks.");

                dynamic qbVendor = JsonConvert.DeserializeObject(deleteJson);
                var localVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VId == vendorId);
                if (localVendor != null)
                {
                    localVendor.SyncToken = qbVendor.Vendor.SyncToken;
                    localVendor.Active = false;
                    localVendor.LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
                        ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
                        : (DateTime?)null;

                    await _context.SaveChangesAsync();
                }

                return new ApiResponse<string>("Vendor deactivated successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(error: ex.Message, message: "Error occurred while deleting vendor.");
            }
        }

        public async Task<ApiResponse<string>> ActivateVendorAsync(string vendorId, string token, string realmId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var qburl = _configuration["QuickBookBaseUrl"];
                var getUrl = $"{qburl}{realmId}/vendor/{vendorId}?minorversion=65";

                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getResponse = await client.SendAsync(getRequest);
                var getJson = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                    return new ApiResponse<string>(error: getJson, message: "Failed to fetch vendor before activation.");

                dynamic existingVendor = JsonConvert.DeserializeObject(getJson);

                var payload = new
                {
                    DisplayName = existingVendor.Vendor.DisplayName,
                    Id = vendorId,
                    SyncToken = existingVendor.Vendor.SyncToken,
                    Active = true
                };

                var json = JsonConvert.SerializeObject(payload);
                var updateRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{qburl}{realmId}/vendor?operation=update&minorversion=65")
                {
                    Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
                };
                updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var updateResponse = await client.SendAsync(updateRequest);
                var updateJson = await updateResponse.Content.ReadAsStringAsync();

                if (!updateResponse.IsSuccessStatusCode)
                    return new ApiResponse<string>(error: updateJson, message: "Failed to activate vendor in QuickBooks.");

                dynamic qbVendor = JsonConvert.DeserializeObject(updateJson);
                var localVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VId == vendorId);

                if (localVendor != null)
                {
                    localVendor.SyncToken = qbVendor.Vendor.SyncToken;
                    localVendor.Active = true;
                    localVendor.LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
                        ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
                        : (DateTime?)null;

                    await _context.SaveChangesAsync();
                }

                return new ApiResponse<string>("Vendor activated successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(error: ex.Message, message: "Error occurred while activating vendor.");
            }
        }

    }
}
