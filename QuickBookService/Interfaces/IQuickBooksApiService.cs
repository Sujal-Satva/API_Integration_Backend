using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedModels.Models;

namespace QuickBookService.Interfaces
{
    public interface IQuickBooksApiService
    {
        Task<CommonResponse<object>> QuickBooksGetRequest(string endpoint,ConnectionModal connection);
        Task<CommonResponse<object>> QuickBooksPostRequest(string endpoint, object payload,ConnectionModal connection);
        
    }
}
