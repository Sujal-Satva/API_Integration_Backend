using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeroService.Interfaces
{
    public interface IXeroCustomerService
    {
        Task<CommonResponse<object>> FetchCustomersFromXero(ConnectionModal connection);
        Task<CommonResponse<object>> AddItem(ConnectionModal connection, CustomerInputModel inputModel);
        Task<CommonResponse<object>> EditCustomer(ConnectionModal connection, string contactId, CustomerInputModel inputModel);

        Task<CommonResponse<object>> UpdateCustomerStatus(ConnectionModal connection, string contactId, string status);
    }
}
