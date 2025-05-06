using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedModels.Models;

namespace XeroService.Interfaces
{
    public interface IXeroApiService
    {
        Task<CommonResponse<object>> XeroGetRequest(string endpoint,ConnectionModal connection);
        Task<CommonResponse<object>> XeroPostRequest(string endpoint, object payload,ConnectionModal connection);
        Task<CommonResponse<object>> XeroDeleteRequest(string endpoint, ConnectionModal connection);
    }
}
