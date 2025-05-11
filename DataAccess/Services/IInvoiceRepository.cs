
using Microsoft.AspNetCore.Http;
using SharedModels.Models;
using task_14.Models;

namespace task_14.Services
{
    public interface IInvoiceRepository
    {

        Task<CommonResponse<object>> SyncInvoices(string platform);
        Task<CommonResponse<object>> AddInvoicesAsync(string platform, object input);

        Task<CommonResponse<object>> EditInvoicesAsync(string platform, string id, object input);

        Task<CommonResponse<object>> DeleteInvoiceAsync(string platform, string id, string status);


        Task<CommonResponse<PagedResponse<InvoiceDto>>> GetInvoicesAsync(
            int page,
            int pageSize,
            string search,
            string sortBy,
            string sortDirection,
            bool pagination = true,
            string source = "all",
            bool isBill=false);


    }
}
