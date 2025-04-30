using task_14.Models;

namespace task_14.Services
{
    public interface IBillRepository
    {
        Task<ApiResponse<string>> SyncBillsFromQuickBooksAsync(string token, string realmId);
        Task<ApiResponse<string>> CreateBillAsync(QuickBooksBillCreateDto billDto, string token, string realmId);
        Task<PagedResponse<object>> GetBillsAsync(int page, int pageSize, string? search, string? sortColumn, string? sortDirection, bool pagination);

    }
}
