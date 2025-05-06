using task_14.Models;
using SharedModels.Models;

namespace task_14.Services
{
    public interface IAuthRepository
    {
        Task<CommonResponse<QuickBooksTokenResponse>> HandleQuickBooksCallbackAsync(string code, string realmId,string state);

        Task<CommonResponse<XeroTokenResponse>> HandleXeroCallbackAsync(string code);
        
    }
}
