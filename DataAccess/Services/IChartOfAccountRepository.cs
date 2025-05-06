using task_14.Models;
using SharedModels.Models;

namespace task_14.Services
{
    public interface IChartOfAccountRepository
    {
        Task<CommonResponse<object>> FetchAndSaveQBOChartOfAccountsAsync();

        Task<CommonResponse<object>> FetchAndSaveXeroChartOfAccountsAsync();
        Task<CommonResponse<PagedResponse<ChartOfAccount>>> GetAccountsFromDbAsync(
           int page = 1,
           int pageSize = 10,
           string? search = null,
           string? sortColumn = "Name",
           string? sortDirection = "asc",
           bool pagination = true,
           string? sourceSystem = null);
        Task<List<AccountViewModel>> GetIncomeAccountsAsync(string realmId, string token);
        Task<List<AccountViewModel>> GetExpenseAccountsAsync(string realmId, string token);

        Task<ProductAccountOptionsDto> GetProductAccountOptionsAsync(int? productId);

    }
}
