using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedModels.Models;
using SharedModels.Xero.Models;

namespace XeroService.Interfaces
{
    public interface IXeroProductService
    {
        Task<CommonResponse<object>> FetchProductsFromXero(ConnectionModal connection);
        Task<CommonResponse<object>> AddItem(ConnectionModal connection, XeroProductInputModel input);

        Task<CommonResponse<object>> EditItem(ConnectionModal connection, string itemId, XeroProductInputModel input);

        Task<CommonResponse<object>> EditXeroItemStatusAsync(ConnectionModal connection, string id, bool status);
    }
}
