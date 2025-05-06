using SharedModels.Models;
using SharedModels.QuickBooks.Models;

namespace QuickBookService.Interfaces
{
    public  interface IQuickBooksInvoiceServices
    {
        Task<CommonResponse<object>> FetchInvoicesFromQuickBooks(ConnectionModal connection);

        Task<CommonResponse<object>> AddInvoice(ConnectionModal connection, UnifiedInvoiceInputModel model);

        Task<CommonResponse<object>> EditInvoice(ConnectionModal connection, string inviceId, UnifiedInvoiceInputModel input);

        Task<CommonResponse<object>> DeleteInvoice(ConnectionModal connection, string invoiceId);
    }
}
