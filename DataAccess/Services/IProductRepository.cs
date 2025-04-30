using task_14.Models;


namespace task_14.Services
{
    public interface IProductRepository
    {
        Task<ApiResponse<Product>> AddProductAsync(string token, string realmId, ProductInputModel inputModel);
        Task<PagedResponse<Product>> GetPagedProductsAsync(
             string search,
             string sortColumn,
             string sortDirection,
             int page,
             int pageSize,
             bool pagination,
             bool active);
        Task<ApiResponse<Product>> UpdateProductAsync(string token, string realmId, int id, ProductInputModel inputModel);

        Task<ApiResponse<string>> MarkProductActiveAsync(string token, string realmId, int customerId);
        Task<ApiResponse<string>> DeleteProductAsync(string token, string realmId, int id);
        Task<bool> SyncItemsFromQuickBooksAsync(string authorization, string realmId);
        Task<int> GetTotalProductCountAsync(string? search);
    }

}
