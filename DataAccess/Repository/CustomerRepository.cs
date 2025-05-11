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
        private readonly IXeroCustomerService _xeroCustomerService;
        private readonly SyncingFunction _syncingFunction;
        private readonly IConnectionRepository _connectionRepository;

        public CustomerRepository(ApplicationDbContext context, SyncingFunction syncingFunction, IConnectionRepository connectionRepository,IQuickBooksCustomerService quickBooksCustomerService,IXeroCustomerService xeroCustomerService)
        {
            _context = context;
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

                CommonResponse<object> response;

                switch (platform.ToLower())
                {
                    case "quickbooks":
                        response = await _quickBooksCustomerService.FetchCustomersFromQuickBooks(connectionResult.Data);
                        break;

                    case "xero":
                        response = await _xeroCustomerService.FetchCustomersFromXero(connectionResult.Data);
                        break;

                    default:
                        return new CommonResponse<object>(400, $"Unsupported platform: {platform}");
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

                    case "phone":
                        query = sortDirection.ToLower() == "desc" ? query.OrderByDescending(c => c.PhoneNumber) :
                            query.OrderBy(c => c.PhoneNumber);
                        break;

                    case "displayname":
                        query = sortDirection.ToLower() == "desc"
                            ? query.OrderByDescending(c => c.DisplayName)
                            : query.OrderBy(c => c.DisplayName);
                        break;

                    default:
                        query = query.OrderByDescending(c => c.LastUpdatedUtc);
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


        public async Task<CommonResponse<object>> AddCustomersAsync(string platform, CustomerInputModel input)
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
                        response = await _xeroCustomerService.AddItem(connectionResult.Data, input);
                        break;

                    case "quickbooks":
                        response = await _quickBooksCustomerService.AddItem(connectionResult.Data, input);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedCustomer> customers && customers.Any())
                {
                    await _syncingFunction.StoreUnifiedCustomersAsync(customers);
                }

                return response;
            }catch(Exception ex)
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
                    case "xero":
                        response = await _xeroCustomerService.UpdateCustomerStatus(connectionResult.Data, id, status);
                        break;

                    case "quickbooks":
                        response = await _quickBooksCustomerService.UpdateCustomerStatus(connectionResult.Data, id, status);
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


    }
}
