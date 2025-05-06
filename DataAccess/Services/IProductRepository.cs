using task_14.Models;
using SharedModels.Models;

namespace task_14.Services
{
    public interface IProductRepository
    {
        //Task<ApiResponse<Product>> AddProductAsync(string token, string realmId, ProductInputModel inputModel);
        //Task<PagedResponse<Product>> GetPagedProductsAsync(
        //     string search,
        //     string sortColumn,
        //     string sortDirection,
        //     int page,
        //     int pageSize,
        //     bool pagination,
        //     bool active);
        //Task<ApiResponse<Product>> UpdateProductAsync(string token, string realmId, int id, ProductInputModel inputModel);
        //Task<ApiResponse<string>> MarkProductActiveAsync(string token, string realmId, int customerId);
        //Task<ApiResponse<string>> DeleteProductAsync(string token, string realmId, int id);
        //Task<bool> SyncItemsFromQuickBooksAsync(string authorization, string realmId);
        //Task<int> GetTotalProductCountAsync(string? search);

        Task<CommonResponse<object>> SyncItems(string platform);

        Task<CommonResponse<PagedResponse<UnifiedItem>>> GetItems(string search,
                   string sortColumn,
                   string sortDirection,
                   int page,
                   int pageSize,
                   bool pagination,
                   bool active,
                   string sourceSystem,
                   string souceType);

        Task<CommonResponse<object>> AddItemsAsync(string platform, object input);

        Task<CommonResponse<object>> EditItemsAsync(string platform, string itemId, object input);

        Task<CommonResponse<object>> EditItemStatusAsync( string id,bool status,string platform);
    }

}
