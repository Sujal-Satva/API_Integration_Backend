
using SharedModels.Models;

namespace DataAccess.Services
{
    public interface IConnectionRepository
    {
        Task<CommonResponse<ConnectionModal>> SaveConnectionAsync(ConnectionModal connection);
        Task<CommonResponse<ConnectionModal>> GetConnectionAsync(string platform);
        Task<CommonResponse<ConnectionModal>> DeleteConnectionAsync(string id);

        Task<CommonResponse<ConnectionModal>> UpdateConnectionAsync(string id, ConnectionModal updatedConnection);
    }
}
