
using Microsoft.AspNetCore.Http;
using task_14.Models;

namespace task_14.Services
{
    public interface IInvoiceRepository
    {
        Task<ApiResponse<object>> SyncInvoicesFromQuickBooksAsync(string token, string realmId);
        Task<PagedResponse<InvoiceDto>> GetInvoicesAsync(int page, int pageSize, string search, string sortBy, string sortDirection,bool pagination);
        Task<ApiResponse<object>> CreateInvoiceAsync(string token, InvoiceInputModel model,string realmId);
        Task<ApiResponse<object>> UpdateInvoiceAsync(string token, int invoiceId, InvoiceInputModel model,string realmId);
        Task<ApiResponse<object>> DeleteInvoiceAsync(string token, int invoiceId, string realmId);

        Task<ApiResponse<object>> ProcessCsvFileAsync(string token, string realmId, IFormFile file);
        //Task<ApiResponse<object>> HandleCSVUpload(string authorization,string realmId, IFormFile file);

    }
}
