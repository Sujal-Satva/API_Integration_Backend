using task_14.Models;

namespace task_14.Services
{
    public interface ITokenRespository
    {
        Task<TokenResult> GetTokenAsync();
    }
}
