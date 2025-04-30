using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using task_14.Data;
using task_14.Models;
using Microsoft.EntityFrameworkCore;
using task_14.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace task_14.Repository
{
    public class TokenService : ITokenRespository

    {
            private readonly ApplicationDbContext _dbContext;
            private readonly ILogger<TokenService> _logger;
            private readonly IConfiguration _configuration;
            private readonly IHttpClientFactory _httpClientFactory;

            public TokenService(
                ApplicationDbContext dbContext,
                ILogger<TokenService> logger,
                IConfiguration configuration,
                IHttpClientFactory httpClientFactory)
            {
                _dbContext = dbContext;
                _logger = logger;
                _configuration = configuration;
                _httpClientFactory = httpClientFactory;
            }

        public async Task<TokenResult> GetTokenAsync()
        {
            var token = await _dbContext.QuickBooksTokens.FirstOrDefaultAsync();

            if (token == null)
            {
                _logger.LogWarning("No token found for realmId");
                return new TokenResult { Success = false };
            }

            if (token.AccessTokenExpiry > DateTime.UtcNow)
            {
                _logger.LogInformation("Access token still valid for realmId");
                return new TokenResult { Success = true, Token = token.AccessToken, RealmId=token.RealmId };
            }

            _logger.LogInformation("Access token expired. Refreshing token for realmId");
            return await RefreshAndSaveTokenAsync(token);
        }

        private async Task<TokenResult> RefreshAndSaveTokenAsync(QuickBooksToken token)
        {
            try
            {
                var clientId = _configuration["QuickBooks:ClientId"];
                var clientSecret = _configuration["QuickBooks:ClientSecret"];
                var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                var client = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", token.RefreshToken }
                    })
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());

                token.AccessToken = json.GetProperty("access_token").GetString();
                token.RefreshToken = json.GetProperty("refresh_token").GetString();
                token.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32());
                token.RefreshTokenExpiry = DateTime.UtcNow.AddSeconds(json.GetProperty("x_refresh_token_expires_in").GetInt32());

                _dbContext.QuickBooksTokens.Update(token);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Token refreshed successfully for realmId: {RealmId}", token.RealmId);
                return new TokenResult { Success = true, Token = token.AccessToken, RealmId = token.RealmId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token for realmId: {RealmId}", token.RealmId);
                return new TokenResult { Success = false };
            }
        }
    }
    }

