using DataAccess.Helper;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using QuickBookService.Interfaces;
using QuickBookService.Services;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using task_14.Data;
using task_14.Models;
using task_14.Services;

namespace task_14.Repository
{
    public class VendorRepository:IVendorRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionRepository _connectionRepository;
        private readonly IQuickBooksVendorService _quickBooksVendorService;
        private readonly SyncingFunction _syncingFunction;

        public VendorRepository(ApplicationDbContext context,SyncingFunction syncingFunction, IConnectionRepository connectionRepository,IQuickBooksVendorService quickBooksVendorService)
        {
            _context = context;
            _connectionRepository = connectionRepository;
            _quickBooksVendorService = quickBooksVendorService;
            _syncingFunction= syncingFunction;
        }

        public async Task<CommonResponse<object>> SyncVendors(string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                CommonResponse<object> response;

                switch (platform.ToLower())
                {
                    case "quickbooks":
                        response = await _quickBooksVendorService.FetchVendorsFromQuickBooks(connectionResult.Data);
                        break;

                    //case "xero":
                    //    response = await _xeroVendorService.FetchVendorsFromXero(connectionResult.Data);
                    //    break;

                    default:
                        return new CommonResponse<object>(400, $"Unsupported platform: {platform}");
                }

                if (response.Data == null)
                    return new CommonResponse<object>(response.Status, response.Message);
                var unifiedVendors = response.Data as List<UnifiedVendor>;
                if (unifiedVendors == null || !unifiedVendors.Any())
                    return new CommonResponse<object>(400, "No vendors found to sync");

                await _syncingFunction.UpdateSyncingInfo(connectionResult.Data.ExternalId, "Vendors", DateTime.UtcNow);
                return await _syncingFunction.StoreUnifiedVendorsAsync(unifiedVendors);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Failed to sync vendors", ex.Message);
            }
        }

        public async Task<CommonResponse<PagedResponse<UnifiedVendor>>> GetAllActiveVendorsAsync(
            string? search,
            string? sortColumn,
            string? sortDirection,
            bool pagination,
            int page,
            int pageSize,
            bool active)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            IQueryable<UnifiedVendor> query = _context.UnifiedVendors.Where(v => v.Active == active);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v =>
                    v.DisplayName.Contains(search) ||
                    v.Phone.Contains(search) ||
                    v.Email.Contains(search) ||
                    v.Website.Contains(search) ||
                    v.CompanyName.Contains(search));
            }
            sortColumn ??= "DisplayName";
            sortDirection ??= "asc";

            query = sortColumn.ToLower() switch
            {
                "displayname" => sortDirection == "desc" ? query.OrderByDescending(v => v.DisplayName) : query.OrderBy(v => v.DisplayName),
                "balance" => sortDirection == "desc" ? query.OrderByDescending(v => v.Balance) : query.OrderBy(v => v.Balance),
                "createdtime" => sortDirection == "desc" ? query.OrderByDescending(v => v.CreateTime) : query.OrderBy(v => v.CreateTime),
                "lastupdatedtime" => sortDirection == "desc" ? query.OrderByDescending(v => v.LastUpdatedTime) : query.OrderBy(v => v.LastUpdatedTime),
                "companyname" => sortDirection == "desc" ? query.OrderByDescending(v => v.CompanyName) : query.OrderBy(v => v.CompanyName),
                _ => sortDirection == "desc" ? query.OrderByDescending(v => v.DisplayName) : query.OrderBy(v => v.DisplayName)
            };

            if (!pagination)
            {
                var all = await query.ToListAsync();
                return new CommonResponse<PagedResponse<UnifiedVendor>>(
                    200,
                    "Vendors fetched successfully",
                    new PagedResponse<UnifiedVendor>(all, page, all.Count, all.Count)
                );
            }

            var total = await query.CountAsync();
            var paged = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new CommonResponse<PagedResponse<UnifiedVendor>>(
                    200,
                    "Vendors fetched successfully",
                    new PagedResponse<UnifiedVendor>(paged, page, pageSize, total)
                );
        }

        public async Task<CommonResponse<object>> AddCustomersAsync(string platform, VendorInputModel input)
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
                    case "quickbooks":
                        response = await _quickBooksVendorService.AddVendor(connectionResult.Data, input);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedVendor> vendors)
                {
                    await _syncingFunction.StoreUnifiedVendorsAsync(vendors);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while adding the item", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> EditCustomersAsync(string platform, string itemId, VendorInputModel input)
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
                 
                    case "quickbooks":
                        response = await _quickBooksVendorService.EditVendor(connectionResult.Data, itemId, input);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedVendor> vendors && vendors.FirstOrDefault() is UnifiedVendor updatedVendor)
                {
                    await _syncingFunction.StoreUnifiedVendorsAsync(vendors);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while editing the item", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> UpdateCustomerStatusAsync(string id, string platform, string status)
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
                   
                    case "quickbooks":
                        response = await _quickBooksVendorService.UpdateVendorStatus(connectionResult.Data, id, status);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedVendor> vendors && vendors.FirstOrDefault() is UnifiedVendor updatedVendor)
                {
                    await _syncingFunction.StoreUnifiedVendorsAsync(vendors);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while editing the item", ex.Message);
            }
        }


        //public async Task<ApiResponse<string>> AddVendorAsync(VendorInputModal inputModel, string token, string realmId)
        //{
        //    try
        //    {
        //        var qburl = _configuration["QuickBookBaseUrl"];
        //        string url = $"{qburl}{realmId}/vendor?minorversion=65";

        //        var payload = new
        //        {
        //            DisplayName = inputModel.DisplayName,
        //            GivenName = inputModel.GivenName,
        //            FamilyName = inputModel.FamilyName,
        //            CompanyName = inputModel.CompanyName,
        //            Active = inputModel.Active,
        //            Balance = inputModel.Balance,   
        //            CurrencyRef = new { value = inputModel.CurrencyValue, name = inputModel.CurrencyName },
        //            BillAddr = new
        //            {
        //                Line1 = inputModel.BillAddrLine1,
        //                City = inputModel.BillAddrCity,
        //                CountrySubDivisionCode = inputModel.BillAddrPostalCode,
        //                PostalCode = inputModel.BillAddrPostalCode
        //            },
        //            PrimaryPhone = new { FreeFormNumber = inputModel.PrimaryPhone },
        //            PrimaryEmailAddr = new { Address = inputModel.PrimaryEmailAddr },
        //            WebAddr = new { URI = inputModel.WebAddr }
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var client = _httpClientFactory.CreateClient();

        //        var request = new HttpRequestMessage(HttpMethod.Post, url)
        //        {
        //            Content = new StringContent(json, Encoding.UTF8, "application/json")
        //        };
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var response = await client.SendAsync(request);
        //        var responseJson = await response.Content.ReadAsStringAsync();

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return new ApiResponse<string>(error: responseJson, message: "Failed to create vendor in QuickBooks");
        //        }

        //        dynamic qbVendor = JsonConvert.DeserializeObject(responseJson);

        //        var createdVendor = new Vendor
        //        {
        //            VId = qbVendor.Vendor.Id,
        //            SyncToken = qbVendor.Vendor.SyncToken,
        //            DisplayName = qbVendor.Vendor.DisplayName,
        //            GivenName = qbVendor.Vendor.GivenName ?? "",
        //            FamilyName = qbVendor.Vendor.FamilyName ?? "",
        //            CompanyName = qbVendor.Vendor.CompanyName ?? "",
        //            Active = qbVendor.Vendor.Active ?? true,
        //            Vendor1099 = qbVendor.Vendor.Vendor1099 ?? false,
        //            Balance = qbVendor.Vendor.Balance ?? 0,
        //            V4IDPseudonym = qbVendor.Vendor.V4IDPseudonym ?? "",
        //            CurrencyValue = qbVendor.Vendor.CurrencyRef?.value ?? "",
        //            CurrencyName = qbVendor.Vendor.CurrencyRef?.name ?? "",
        //            BillAddrLine1 = qbVendor.Vendor.BillAddr?.Line1 ?? "",
        //            BillAddrCity = qbVendor.Vendor.BillAddr?.City ?? "",
        //            BillAddrPostalCode = qbVendor.Vendor.BillAddr?.CountrySubDivisionCode ?? "",
        //            PrimaryPhone = qbVendor.Vendor.PrimaryPhone?.FreeFormNumber ?? "",
        //            PrimaryEmailAddr = qbVendor.Vendor.PrimaryEmailAddr?.Address ?? "",
        //            WebAddr = qbVendor.Vendor.WebAddr?.URI ?? "",
        //            CreateTime = qbVendor.Vendor.MetaData?.CreateTime != null
        //                ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.CreateTime.ToString()).UtcDateTime
        //                : (DateTime?)null,
        //            LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
        //                ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
        //                : (DateTime?)null
        //        };

        //        await _context.Vendors.AddAsync(createdVendor);
        //        await _context.SaveChangesAsync();

        //        return new ApiResponse<string>("Vendor added successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<string>(error: ex.Message, message: "Error occurred while adding vendor.");
        //    }
        //}

        //public async Task<ApiResponse<string>> UpdateVendorAsync(string vendorId, VendorInputModal inputModel, string token, string realmId)
        //{
        //    try
        //    {
        //        var client = _httpClientFactory.CreateClient();

        //        var qburl = _configuration["QuickBookBaseUrl"];
        //        var getUrl = $"{qburl}{realmId}/vendor/{vendorId}?minorversion=65";
        //        var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
        //        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var getResponse = await client.SendAsync(getRequest);
        //        var getJson = await getResponse.Content.ReadAsStringAsync();

        //        if (!getResponse.IsSuccessStatusCode)
        //        {
        //            return new ApiResponse<string>(error: getJson, message: "Failed to fetch vendor before update.");
        //        }

        //        dynamic existingVendor = JsonConvert.DeserializeObject(getJson);

        //        // Step 2: Prepare update payload
        //        var payload = new
        //        {
        //            Id = vendorId,
        //            SyncToken = existingVendor.Vendor.SyncToken,
        //            DisplayName = inputModel.DisplayName,
        //            GivenName = inputModel.GivenName,
        //            FamilyName = inputModel.FamilyName,
        //            CompanyName = inputModel.CompanyName,
        //            Active = inputModel.Active,
        //            Balance = inputModel.Balance,
        //            CurrencyRef = new { value = inputModel.CurrencyValue, name = inputModel.CurrencyName },
        //            BillAddr = new
        //            {
        //                Line1 = inputModel.BillAddrLine1,
        //                City = inputModel.BillAddrCity,
        //                CountrySubDivisionCode = inputModel.BillAddrPostalCode,
        //                PostalCode = inputModel.BillAddrPostalCode
        //            },
        //            PrimaryPhone = new { FreeFormNumber = inputModel.PrimaryPhone },
        //            PrimaryEmailAddr = new { Address = inputModel.PrimaryEmailAddr },
        //            WebAddr = new { URI = inputModel.WebAddr }
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var updateRequest = new HttpRequestMessage(HttpMethod.Post, $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/vendor?minorversion=65")
        //        {
        //            Content = new StringContent(json, Encoding.UTF8, "application/json")
        //        };
        //        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var updateResponse = await client.SendAsync(updateRequest);
        //        var updateJson = await updateResponse.Content.ReadAsStringAsync();

        //        if (!updateResponse.IsSuccessStatusCode)
        //        {
        //            return new ApiResponse<string>(error: updateJson, message: "Failed to update vendor in QuickBooks.");
        //        }

        //        dynamic qbVendor = JsonConvert.DeserializeObject(updateJson);

        //        // Update local DB
        //        var localVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VId == vendorId);
        //        if (localVendor != null)
        //        {
        //            localVendor.DisplayName = qbVendor.Vendor.DisplayName;
        //            localVendor.GivenName = qbVendor.Vendor.GivenName ?? "";
        //            localVendor.FamilyName = qbVendor.Vendor.FamilyName ?? "";
        //            localVendor.CompanyName = qbVendor.Vendor.CompanyName ?? "";
        //            localVendor.Active = qbVendor.Vendor.Active ?? true;
        //            localVendor.Vendor1099 = qbVendor.Vendor.Vendor1099 ?? false;
        //            localVendor.Balance = qbVendor.Vendor.Balance ?? 0;
        //            localVendor.V4IDPseudonym = qbVendor.Vendor.V4IDPseudonym ?? "";
        //            localVendor.CurrencyValue = qbVendor.Vendor.CurrencyRef?.value ?? "";
        //            localVendor.CurrencyName = qbVendor.Vendor.CurrencyRef?.name ?? "";
        //            localVendor.BillAddrLine1 = qbVendor.Vendor.BillAddr?.Line1 ?? "";
        //            localVendor.BillAddrCity = qbVendor.Vendor.BillAddr?.City ?? "";
        //            localVendor.BillAddrPostalCode = qbVendor.Vendor.BillAddr?.CountrySubDivisionCode ?? "";
        //            localVendor.PrimaryPhone = qbVendor.Vendor.PrimaryPhone?.FreeFormNumber ?? "";
        //            localVendor.PrimaryEmailAddr = qbVendor.Vendor.PrimaryEmailAddr?.Address ?? "";
        //            localVendor.WebAddr = qbVendor.Vendor.WebAddr?.URI ?? "";
        //            localVendor.LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
        //                ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
        //                : (DateTime?)null;

        //            await _context.SaveChangesAsync();
        //        }

        //        return new ApiResponse<string>("Vendor updated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<string>(error: ex.Message, message: "Error occurred while updating vendor.");
        //    }
        //}

        //public async Task<ApiResponse<string>> DeactivateVendorAsync(string vendorId, string token, string realmId)
        //{
        //    try
        //    {
        //        var client = _httpClientFactory.CreateClient();
        //        var qburl = _configuration["QuickBookBaseUrl"];
        //        var getUrl = $"{qburl}{realmId}/vendor/{vendorId}?minorversion=65";
        //        var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
        //        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var getResponse = await client.SendAsync(getRequest);
        //        var getJson = await getResponse.Content.ReadAsStringAsync();

        //        if (!getResponse.IsSuccessStatusCode)
        //            return new ApiResponse<string>(error: getJson, message: "Failed to fetch vendor before delete.");

        //        dynamic existingVendor = JsonConvert.DeserializeObject(getJson);
        //        var payload = new
        //        {
        //            DisplayName = existingVendor.Vendor.DisplayName,
        //            Id = vendorId,
        //            SyncToken = existingVendor.Vendor.SyncToken,
        //            Active = false
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var deleteRequest = new HttpRequestMessage(
        //            HttpMethod.Post,
        //            $"{qburl}{realmId}/vendor?operation=update&minorversion=65")
        //        {
        //            Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
        //        };
        //        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        deleteRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var deleteResponse = await client.SendAsync(deleteRequest);
        //        var deleteJson = await deleteResponse.Content.ReadAsStringAsync();

        //        if (!deleteResponse.IsSuccessStatusCode)
        //            return new ApiResponse<string>(error: deleteJson, message: "Failed to deactivate vendor in QuickBooks.");

        //        dynamic qbVendor = JsonConvert.DeserializeObject(deleteJson);
        //        var localVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VId == vendorId);
        //        if (localVendor != null)
        //        {
        //            localVendor.SyncToken = qbVendor.Vendor.SyncToken;
        //            localVendor.Active = false;
        //            localVendor.LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
        //                ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
        //                : (DateTime?)null;

        //            await _context.SaveChangesAsync();
        //        }

        //        return new ApiResponse<string>("Vendor deactivated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<string>(error: ex.Message, message: "Error occurred while deleting vendor.");
        //    }
        //}

        //public async Task<ApiResponse<string>> ActivateVendorAsync(string vendorId, string token, string realmId)
        //{
        //    try
        //    {
        //        var client = _httpClientFactory.CreateClient();
        //        var qburl = _configuration["QuickBookBaseUrl"];
        //        var getUrl = $"{qburl}{realmId}/vendor/{vendorId}?minorversion=65";

        //        var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
        //        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var getResponse = await client.SendAsync(getRequest);
        //        var getJson = await getResponse.Content.ReadAsStringAsync();

        //        if (!getResponse.IsSuccessStatusCode)
        //            return new ApiResponse<string>(error: getJson, message: "Failed to fetch vendor before activation.");

        //        dynamic existingVendor = JsonConvert.DeserializeObject(getJson);

        //        var payload = new
        //        {
        //            DisplayName = existingVendor.Vendor.DisplayName,
        //            Id = vendorId,
        //            SyncToken = existingVendor.Vendor.SyncToken,
        //            Active = true
        //        };

        //        var json = JsonConvert.SerializeObject(payload);
        //        var updateRequest = new HttpRequestMessage(
        //            HttpMethod.Post,
        //            $"{qburl}{realmId}/vendor?operation=update&minorversion=65")
        //        {
        //            Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
        //        };
        //        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //        updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var updateResponse = await client.SendAsync(updateRequest);
        //        var updateJson = await updateResponse.Content.ReadAsStringAsync();

        //        if (!updateResponse.IsSuccessStatusCode)
        //            return new ApiResponse<string>(error: updateJson, message: "Failed to activate vendor in QuickBooks.");

        //        dynamic qbVendor = JsonConvert.DeserializeObject(updateJson);
        //        var localVendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VId == vendorId);

        //        if (localVendor != null)
        //        {
        //            localVendor.SyncToken = qbVendor.Vendor.SyncToken;
        //            localVendor.Active = true;
        //            localVendor.LastUpdatedTime = qbVendor.Vendor.MetaData?.LastUpdatedTime != null
        //                ? DateTimeOffset.Parse(qbVendor.Vendor.MetaData.LastUpdatedTime.ToString()).UtcDateTime
        //                : (DateTime?)null;

        //            await _context.SaveChangesAsync();
        //        }

        //        return new ApiResponse<string>("Vendor activated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<string>(error: ex.Message, message: "Error occurred while activating vendor.");
        //    }
        //}

    }
}
