using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using task_14.Models;

namespace task_14.Services
{
    public interface IConnectionRepository
    {
        Task<CommonResponse<ConnectionModal>> SaveConnectionAsync(ConnectionModal connection);
        Task<CommonResponse<ConnectionModal>> GetConnectionAsync(string platform);

        Task<CommonResponse<ConnectionModal>> DeleteConnectionAsync(string id);
    }
}
