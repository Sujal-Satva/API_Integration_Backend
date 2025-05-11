using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using task_14.Models;
namespace task_14.Services
{
    public interface ICustomerRepository
    {
      
        Task<CommonResponse<PagedResponse<CustomerDTO>>> GetCustomersFromDbAsync(
           int page = 1,
           int pageSize = 10,
           string? search = null,
           string? sortColumn = "DisplayName",
           string? sortDirection = "asc",
           string? sourceSystem = null,
           bool active = true,
           bool pagination = true);

        Task<CommonResponse<object>> SyncCustomers(string platform);
        Task<CommonResponse<object>> AddCustomersAsync(string platform, CustomerInputModel input);

        Task<CommonResponse<object>> EditCustomersAsync(string platform, string itemId, CustomerInputModel input);

        Task<CommonResponse<object>> UpdateCustomerStatusAsync(string id, string platform, string status);


      
    }

}
