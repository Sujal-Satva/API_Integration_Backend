using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using task_14.Data;
using task_14.Models;
using task_14.Services;


namespace task_14.Repository
{
    public class ConnectionRepository:IConnectionRepository
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConnectionRepository> _logger;

        public ConnectionRepository(ApplicationDbContext context, ILogger<ConnectionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CommonResponse<ConnectionModal>> SaveConnectionAsync(ConnectionModal connection)
        {
            try
            {
                var existingConnection = await _context.Connections
                    .FirstOrDefaultAsync(c => c.SourceAccounting == connection.SourceAccounting && c.ExternalId == connection.ExternalId);

                if (existingConnection != null)
                {
                    return new CommonResponse<ConnectionModal>(409,
                        $"A {connection.SourceAccounting} connection with ID {connection.ExternalId} already exists",
                        existingConnection);
                }

                connection.CreatedAt = DateTime.UtcNow;
                connection.UpdatedAt = DateTime.UtcNow;
                _context.Connections.Add(connection);
                await _context.SaveChangesAsync();

                return new CommonResponse<ConnectionModal>(201,
                    $"{connection.SourceAccounting} connection saved successfully",
                    connection);
            }   
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving {connection.SourceAccounting} connection");
                return new CommonResponse<ConnectionModal>(500, "An error occurred while saving the connection");
            }
        }


        public async Task<CommonResponse<ConnectionModal>> GetConnectionAsync(string platform)
        {
            try
            {
                var connection = await _context.Connections.FirstOrDefaultAsync(c => c.SourceAccounting == platform);
                if (connection == null)
                {
                    return new CommonResponse<ConnectionModal>(401, $"No {platform} connection found");
                }
                return new CommonResponse<ConnectionModal>(200, "Connection retrieved successfully", connection);
            }
            catch (Exception ex) { 
            
                _logger.LogError(ex, $"Error retrieving {platform} connection");
                return new CommonResponse<ConnectionModal>(500, "An error occurred while retrieving the connection");
            }
        }

        public async Task<CommonResponse<ConnectionModal>> DeleteConnectionAsync(string id)
        {
            try
            {
                var connection = await _context.Connections.FindAsync(int.Parse(id));
                if (connection == null)
                {
                    return new CommonResponse<ConnectionModal>(401, $"No  connection found");
                }

                _context.Connections.Remove(connection);
                await _context.SaveChangesAsync();

                return new CommonResponse<ConnectionModal>(200, $"connection Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting connection with ID {id}");
                return new CommonResponse<ConnectionModal>(500, "An error occurred while delete the connection");
            }
        }

    }
}
