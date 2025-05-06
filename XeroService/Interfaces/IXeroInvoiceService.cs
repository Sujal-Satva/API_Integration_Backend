using SharedModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeroService.Interfaces
{
    public interface IXeroInvoiceService
    {
        Task<CommonResponse<object>> FetchInvoicesFromXero(ConnectionModal connection);

        Task<CommonResponse<object>> AddInvoice(ConnectionModal connection, UnifiedInvoiceInputModel input);

        Task<CommonResponse<object>> EditInvoice(ConnectionModal connection, string invoiceId, UnifiedInvoiceInputModel input);

        Task<CommonResponse<object>> DeleteInvoice(ConnectionModal connection, string invoiceNumber, string status);
    }
}
