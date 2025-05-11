using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using task_14.Models;

namespace task_14.Services
{
    public interface IVendorRepository
    {
        Task<CommonResponse<object>> SyncVendors(string platform);
        Task<CommonResponse<PagedResponse<UnifiedVendor>>> GetAllActiveVendorsAsync(
           string? search,
           string? sortColumn,
           string? sortDirection,
           bool pagination,
           int page,
           int pageSize,
           bool active);

        Task<CommonResponse<object>> AddCustomersAsync(string platform, VendorInputModel input);

        Task<CommonResponse<object>> EditCustomersAsync(string platform, string itemId, VendorInputModel input);

        Task<CommonResponse<object>> UpdateCustomerStatusAsync(string id, string platform, string status);
    }
}
