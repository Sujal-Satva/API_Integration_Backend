using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedModels.QuickBooks.Models;
using SharedModels.Xero.Models;
using Newtonsoft.Json;
using SharedModels.Models;
using DataAccess.Helper;
using QuickBookService.Interfaces;
using System.Text.Json;
using task_14.Data;
using task_14.Models;
using task_14.Services;
using DataAccess.Services;
using XeroService.Interfaces;


namespace task_14.Repository
{
    public class ProductRepository: IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly SyncingFunction _syncingFunction;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IQuickBooksProductService _quickBooksProductService;
        private readonly IXeroProductService _xeroProductService;
        private readonly IConnectionRepository _connectionRepository;

        public ProductRepository(ApplicationDbContext context, IHttpClientFactory httpClientFactory,SyncingFunction syncingFunction, IConfiguration configuration,IQuickBooksProductService quickBooksProductService,IConnectionRepository connectionRepository,IXeroProductService xeroProductService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _quickBooksProductService= quickBooksProductService;
            _connectionRepository = connectionRepository;
            _xeroProductService= xeroProductService;
            _syncingFunction = syncingFunction;
        }
        
        public async Task<CommonResponse<object>> SyncItems(string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);
                CommonResponse<object> response = null;

                if (platform == "QuickBooks")
                {
                    response = await _quickBooksProductService.FetchProductsFromQuickBooks(connectionResult.Data);
                }
                else
                {
                    response = await _xeroProductService.FetchProductsFromXero(connectionResult.Data);
                }
                if (response.Data == null)
                    return new CommonResponse<object>(response.Status, response.Message);
                var unifiedItems = response.Data as List<UnifiedItem>;
                await _syncingFunction.UpdateSyncingInfo(connectionResult.Data.ExternalId, "Products", DateTime.UtcNow);
                return await _syncingFunction.StoreUnifiedItemsAsync(unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Failed to insert items", ex.Message);
            }
        }
        


        public async Task<CommonResponse<object>> DeleteUnifiedItemAsync(string id)
        {
            var item = await _context.UnifiedItems.FirstOrDefaultAsync(i => i.ExternalId == id) ;
            if (item == null)
            {
                return new CommonResponse<object>(404, "Item not found");
            }

            _context.UnifiedItems.Remove(item);
            await _context.SaveChangesAsync();

            return new CommonResponse<object>(200, "Item deleted successfully", id);
        }
        public async Task<CommonResponse<object>> AddItemsAsync(string platform, object input)
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
                        var xeroModel = JsonConvert.DeserializeObject<XeroProductInputModel>(input.ToString());
                        response = await _xeroProductService.AddItem(connectionResult.Data, xeroModel);
                        break;

                    case "quickbooks":
                        var qbModel = JsonConvert.DeserializeObject<QuickBooksProductInputModel>(input.ToString());
                        response = await _quickBooksProductService.AddItem(connectionResult.Data, qbModel);
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

        public async Task<CommonResponse<object>> EditItemsAsync(string platform, string itemId, object input)
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
                        var xeroModel = JsonConvert.DeserializeObject<XeroProductInputModel>(input.ToString());
                        response = await _xeroProductService.EditItem(connectionResult.Data, itemId, xeroModel);
                        break;

                    case "quickbooks":
                        var qbModel = JsonConvert.DeserializeObject<QuickBooksProductInputModel>(input.ToString());
                        response = await _quickBooksProductService.EditItem(connectionResult.Data, itemId, qbModel);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedItem> items && items.FirstOrDefault() is UnifiedItem updatedItem)
                {
                    await _syncingFunction.StoreUnifiedItemsAsync(items);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while editing the item", ex.Message);
            }
        }


        public async Task<CommonResponse<PagedResponse<UnifiedItem>>> GetItems(
             string search,
             string sortColumn,
             string sortDirection,
             int page,
             int pageSize,
             bool pagination=true,
             bool active=true,
             string sourceSystem="all",
             string sourceType="all")
        {
            try
            {
                var query = _context.UnifiedItems.AsQueryable();
                query = query.Where(item => item.IsActive == active);
                if (!string.IsNullOrWhiteSpace(sourceSystem) && sourceSystem.ToLower() != "all")
                {
                    query = query.Where(item => item.SourceSystem.ToLower() == sourceSystem.ToLower());
                }
                if (!string.IsNullOrEmpty(sourceType) && sourceType.ToLower() != "all")
                {
                    if (sourceType.ToLower() == "service")
                        query = query.Where(item => item.IsTrackedAsInventory == false);
                    else
                        query = query.Where(item => item.IsTrackedAsInventory == true);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(item =>
                        item.Name.ToLower().Contains(search) ||
                        (item.Description != null && item.Description.ToLower().Contains(search))
                    );
                }
                if (!string.IsNullOrWhiteSpace(sortColumn))
                {
                    query = ApplySorting(query, sortColumn, sortDirection);
                }
                else
                {
                    query = query.OrderBy(item => item.Name);
                }
                int totalRecords = await query.CountAsync();
                if (pagination)
                {
                    query = query.Skip((page - 1) * pageSize).Take(pageSize);
                }

                var result = await query.ToListAsync();

                return new CommonResponse<PagedResponse<UnifiedItem>>(
                    200,
                    "Items fetched successfully",
                    new PagedResponse<UnifiedItem>(result, page, pageSize, totalRecords)
                );
            }
            catch (Exception ex)
            {
                return new CommonResponse<PagedResponse<UnifiedItem>>(
                    500,
                    "Error occurred while fetching items",
                    null
                );
            }
        }

        private IQueryable<UnifiedItem> ApplySorting(IQueryable<UnifiedItem> query, string sortColumn, string sortDirection)
        {
            bool isDescending = sortDirection?.ToLower() == "desc";

            switch (sortColumn.ToLower())
            {
                case "name":
                    return isDescending ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name);
                case "salesunitprice":
                    return isDescending ? query.OrderByDescending(i => i.SalesUnitPrice) : query.OrderBy(i => i.SalesUnitPrice);
                case "purchaseunitprice":
                    return isDescending ? query.OrderByDescending(i => i.PurchaseUnitPrice) : query.OrderBy(i => i.PurchaseUnitPrice);
                case "price":
                    return isDescending ? query.OrderByDescending(i => i.SalesUnitPrice) : query.OrderBy(i => i.SalesUnitPrice);
                case "updatedat":
                    return isDescending ? query.OrderByDescending(i => i.UpdatedAt) : query.OrderBy(i => i.UpdatedAt);
                default:
                    return query.OrderBy(i => i.Name);
            }
        }

        public async Task<CommonResponse<object>> EditItemStatusAsync(string id, bool status, string platform)
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
                        response = await _xeroProductService.EditXeroItemStatusAsync(connectionResult.Data, id, status);
                        break;

                    case "quickbooks":
                        response = await _quickBooksProductService.EditQuickBookItemStatusAsync(connectionResult.Data, id, status);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedItem> items && items.FirstOrDefault() is UnifiedItem updatedItem && items.Any())
                {
                    await _syncingFunction.StoreUnifiedItemsAsync(items);
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
