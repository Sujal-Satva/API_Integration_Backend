using SharedModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeroService.Interfaces
{
    public interface IXeroBillService
    {
        Task<CommonResponse<object>> FetchBillsFromXero(ConnectionModal connection);

        Task<CommonResponse<object>> AddBill(ConnectionModal connection, UnifiedInvoiceInputModel input);
    }
}
