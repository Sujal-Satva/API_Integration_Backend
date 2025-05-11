

using SharedModels.Models;
using SharedModels.QuickBooks.Models;

namespace QuickBookService.Interfaces
{
    public interface IQuickBooksVendorService
    {
        Task<CommonResponse<object>> FetchVendorsFromQuickBooks(ConnectionModal connection);

        Task<CommonResponse<object>> AddVendor(ConnectionModal connection, VendorInputModel inputModel);

        Task<CommonResponse<object>> GetVendorById(ConnectionModal connection, string vendorId);

        Task<CommonResponse<object>> EditVendor(ConnectionModal connection, string vendorId, VendorInputModel inputModel);
        Task<CommonResponse<object>> UpdateVendorStatus(ConnectionModal connection, string vendorId, string status);
    }
}
