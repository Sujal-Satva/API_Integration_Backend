using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickBookService.Interfaces
{
    public interface IQuickBooksCustomerService
    {
        Task<CommonResponse<object>> FetchCustomersFromQuickBooks(ConnectionModal connection);

        Task<CommonResponse<object>> AddItem(ConnectionModal connection, CustomerInputModel inputModel);

        Task<CommonResponse<object>> EditCustomer(ConnectionModal connection, string itemId, CustomerInputModel input);

        Task<CommonResponse<object>> GetItemById(ConnectionModal connection, string itemId);
        //Task<CommonResponse<object>> AddCustomer(ConnectionModal connection, QuickBooksProductInputModel input);

        //Task<CommonResponse<object>> EditCusomter(ConnectionModal connection, string itemId, QuickBooksProductInputModel input);

        //Task<CommonResponse<object>> EditQuickBookCustomerStatusAsync(ConnectionModal connection, string itemId, bool status);
    }
}
