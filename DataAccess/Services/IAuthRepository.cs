using task_14.Models;

namespace task_14.Services
{
    public interface IAuthRepository
    {
        Task<CommonResponse<QuickBooksTokenResponse>> HandleQuickBooksCallbackAsync(string code, string realmId,string state);

        Task<CommonResponse<XeroTokenResponse>> HandleXeroCallbackAsync(string code);
        //Task<(bool Success, string Message, object Result)> HandleQuickBooksCallbackAsync(string code, string realmId);
        //Task<bool> RevokeTokenAsync(string accessToken);
        //Task<(string AccessToken, string RefreshToken)> RefreshQuickBooksTokenAsync(string oldRefreshToken,string realmId);
    }
}
