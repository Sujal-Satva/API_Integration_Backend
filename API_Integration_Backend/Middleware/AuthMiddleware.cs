using Microsoft.EntityFrameworkCore;
using task_14.Data;
using System.Text;
using task_14.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using SharedModels.Models;

namespace task_14.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<TokenRefreshMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public TokenRefreshMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory, ILogger<TokenRefreshMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var connections = await dbContext.Connections
                    .Where(c => c.TokenJson != null)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                if (connections == null || !connections.Any())
                {
                    _logger.LogWarning("No active connections found.");
                    await _next(context);
                    return;
                }

                try
                {
                    foreach (var connection in connections)
                    {
                        if (connection.SourceAccounting.Equals("QuickBooks", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = System.Text.Json.JsonSerializer.Deserialize<QuickBooksToken>(connection.TokenJson);

                            if (token != null && token.AccessTokenExpiry > DateTime.UtcNow)
                            {
                                _logger.LogInformation("QuickBooks access token is still valid for RealmId: {RealmId}", token.RealmId);
                            }
                            else if (token != null)
                            {
                                bool refreshed = await RefreshQuickBooksTokenAsync(dbContext, token, connection);
                                if (refreshed)
                                {
                                    _logger.LogInformation("QuickBooks token refreshed successfully.");
                                }
                            }
                        }
                        else if (connection.SourceAccounting.Equals("Xero", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = System.Text.Json.JsonSerializer.Deserialize<XeroToken>(connection.TokenJson);

                            
                            if (token != null && token.AccessTokenExpiry > DateTime.UtcNow)
                            {
                                _logger.LogInformation("Xero access token is still valid for TenantId: {TenantId}", token.TenantId);
                            }
                            else if (token != null)
                            {
                                bool refreshed = await RefreshXeroTokenAsync(dbContext, token, connection);
                                if (refreshed)
                                {
                                    _logger.LogInformation("Xero token refreshed successfully.");
                                }
                            }
                        }
                    }
                    await _next(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in TokenRefreshMiddleware");
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            status = 500,
                            message = "An error occurred processing your request.",
                            data = (object)null
                        }));
                    }
                    else
                    {
                        _logger.LogWarning("Error occurred but response already started");
                    }
                }
            }
        }


        private async Task<bool> RefreshQuickBooksTokenAsync(ApplicationDbContext dbContext, QuickBooksToken token, ConnectionModal connection)
        {
            try
            {
                var clientId = _configuration["QuickBooks:ClientId"];
                var clientSecret = _configuration["QuickBooks:ClientSecret"];
                var redirectUri = _configuration["QuickBooks:RedirectUri"];
                var tokenUrl = _configuration["QuickBookTokenUrl"];

                using var httpClient = new HttpClient();

                var requestBody = new Dictionary<string, string>
                    {
                            { "grant_type", "refresh_token" },
                            { "refresh_token", token.RefreshToken },
                            { "redirect_uri", redirectUri }
                    };

                var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(requestBody));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<dynamic>(content);

                    string accessToken = tokenResponse.access_token;
                    string refreshToken = tokenResponse.refresh_token;
                    int expiresIn = tokenResponse.expires_in;
                    DateTime expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                    var qboTokenResponse = new QuickBooksTokenResponse
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        RealmId = token.RealmId,
                        AccessTokenExpiry = expiresAt,
                        RefreshTokenExpiry = DateTime.UtcNow.AddDays(60)
                    };
                  
                    var serializedToken = System.Text.Json.JsonSerializer.Serialize(qboTokenResponse);
                    var updatedAt = DateTime.UtcNow;

                    dbContext.Attach(connection);
                    connection.TokenJson = serializedToken;
                    connection.UpdatedAt = updatedAt;

                    dbContext.Entry(connection).Property(c => c.TokenJson).IsModified = true;
                    dbContext.Entry(connection).Property(c => c.UpdatedAt).IsModified = true;

                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation("QuickBooks token refreshed successfully.");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to refresh QuickBooks token. Status code: {StatusCode}", response.StatusCode);
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing QuickBooks token.");
                return false;
            }
        }

        private async Task<bool> RefreshXeroTokenAsync(ApplicationDbContext dbContext, XeroToken token, ConnectionModal connection)
        {
            try
            {
                var tokenEndpoint = _configuration["Xero:TokenEndpoint"];
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", token.RefreshToken),
                    new KeyValuePair<string, string>("client_id", _configuration["Xero:ClientId"]),
                    new KeyValuePair<string, string>("client_secret", _configuration["Xero:ClientSecret"])
                });

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(tokenEndpoint, formContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                        var accessToken = tokenResponse.AccessToken;
                        var refreshToken = tokenResponse.RefreshToken;
                        var accessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiry);
                        var refreshTokenExpiry = DateTime.UtcNow.AddDays(60);
                        var xeroTokenResponse = new XeroTokenResponse
                        {
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            TenantId = token.TenantId,
                            TenantName = token.TenantName,
                            AccessTokenExpiry = accessTokenExpiry,
                            RefreshTokenExpiry = refreshTokenExpiry
                        };

                        dbContext.Attach(connection);
                        connection.TokenJson = JsonConvert.SerializeObject(xeroTokenResponse);
                        connection.ExternalId = token.TenantId;
                        connection.ExternalName = token.TenantName;
                      
                        dbContext.Entry(connection).Property(c => c.TokenJson).IsModified = true;
                        dbContext.Entry(connection).Property(c => c.ExternalId).IsModified = true;
                        dbContext.Entry(connection).Property(c => c.ExternalName).IsModified = true;
                        dbContext.Entry(connection).Property(c => c.UpdatedAt).IsModified = true;

                        await dbContext.SaveChangesAsync();

                        _logger.LogInformation("Xero token refreshed and connection updated successfully.");
                        return true;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Failed to refresh Xero token. Status code: {StatusCode}, Response: {Error}", response.StatusCode, error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Xero token.");
                return false;
            }
        }


    }
}