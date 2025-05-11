using SharedModels.Models;
using task_14.Models;

namespace task_14.Services
{
    public interface IBillRepository
    {
        Task<CommonResponse<object>> SyncBills(string platform);

        Task<CommonResponse<PagedResponse<object>>> GetBillsAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortColumn = null,
            string? sortDirection = "asc",
            bool pagination = true,
            string sourceSystem = "all"
        );

        Task<CommonResponse<object>> AddBillsAsync(string platform, object input);

    }
}
