using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;

namespace QuickBookService.Interfaces
{
    public interface IQuickBooksProductService
    {
        Task<CommonResponse<object>> FetchProductsFromQuickBooks(ConnectionModal connection);

        Task<CommonResponse<object>> AddItem(ConnectionModal connection, QuickBooksProductInputModel input);

        Task<CommonResponse<object>> EditItem(ConnectionModal connection, string itemId, QuickBooksProductInputModel input);

        Task<CommonResponse<object>> EditQuickBookItemStatusAsync(ConnectionModal connection, string itemId, bool status);
    }
}
