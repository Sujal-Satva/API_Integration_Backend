using task_14.Models;

namespace task_14.Services
{
    public interface IVendorRepository
    {
        Task<List<Vendor>> SyncVendorsFromQuickBooksAsync(string token, string realmId);
        Task<ApiResponse<string>> AddVendorAsync(VendorInputModal inputModel, string token, string realmId);
        Task<ApiResponse<string>> DeactivateVendorAsync(string vendorId, string token, string realmId);
        Task<ApiResponse<string>> ActivateVendorAsync(string vendorId, string token, string realmId);
        Task<ApiResponse<string>> UpdateVendorAsync(string vendorId, VendorInputModal inputModel, string token, string realmId);
        Task<PagedResponse<Vendor>> GetAllActiveVendorsAsync(string? search, string? sortColumn, string? sortDirection, bool pagination, int page, int pageSize,bool active);
    }
}
