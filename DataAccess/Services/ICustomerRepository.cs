using task_14.Models;

namespace task_14.Services
{
    public interface ICustomerRepository
    {
        //Task<IEnumerable<Customer>> GetCustomersAsync(int page, int pageSize, string? search, string sortColumn, string sortDirection, bool pagination,bool active);
        //Task<ApiResponse<string>> AddCustomerAsync(string token, string realmId, CustomerInputModel inputModel);
        //Task<ApiResponse<string>> UpdateCustomerAsync(string token, string realmId, string id, CustomerInputModel inputModel);
        //Task<ApiResponse<string>> DeleteCustomerAsync(string token, string realmId, string id);
        //Task<Customer> GetCustomerByIdAsync(string id);
        //Task SyncCustomersFromQuickBooksAsync(string token, string realmId);
        //Task<bool> IsCustomerExistsAsync(string qbId);
        //Task<ApiResponse<string>> MarkCustomerActiveAsync(string token, string realmId, int customerId);
        //public Task<int> GetTotalCustomerCountAsync(string? search,bool active);



        Task<CommonResponse<object>> FetchAndSaveQBOCustomerAsync();

        Task<CommonResponse<object>> FetchAndSaveXeroCustomerAsync();


        Task<CommonResponse<PagedResponse<CustomerDTO>>> GetCustomersFromDbAsync(
           int page = 1,
           int pageSize = 10,
           string? search = null,
           string? sortColumn = "DisplayName",
           string? sortDirection = "asc",
           string? sourceSystem = null,
           bool active = true,
           bool pagination = true);

        Task<CommonResponse<object>> AddCustomerAsync(CustomerInputModel model, string platform);

        Task<CommonResponse<object>> EditCustomerAsync(CustomerInputModel model, string platform);

        Task<CommonResponse<object>> DeleteCustomerAsync(string id, string platform);

        Task<CommonResponse<object>> UpdateStatus(string id, string platform, bool status);

    }

}
