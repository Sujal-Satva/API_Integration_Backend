using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using task_14.Data;
using task_14.Models;
using task_14.Services;
using SharedModels.Models;


namespace task_14.Repository
{
    public class AuthRepository:IAuthRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IConnectionRepository _connectionRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthRepository> _logger;
        private readonly ApplicationDbContext _context;

        public AuthRepository(
                IConfiguration configuration,
                IHttpClientFactory httpClient,
                ApplicationDbContext context,
                ILogger<AuthRepository> logger,
                IConnectionRepository connectionRepository)
        {
            _configuration = configuration;
            _httpClientFactory = httpClient;
            _connectionRepository = connectionRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<CommonResponse<QuickBooksTokenResponse>> HandleQuickBooksCallbackAsync(string code, string realmId,string state)
        {
            try
            {
                var clientId = _configuration["QuickBooks:ClientId"];
                var clientSecret = _configuration["QuickBooks:ClientSecret"];
                var redirectUri = _configuration["QuickBooks:RedirectUri"];

                var httpClient = _httpClientFactory.CreateClient();
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", redirectUri }
                });

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
                var tokenUrl = _configuration["QuickBookTokenUrl"];
                var response = await httpClient.PostAsync(tokenUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"QuickBooks token exchange failed: {errorContent}");
                    return new CommonResponse<QuickBooksTokenResponse>((int)response.StatusCode, $"Failed to exchange authorization code for tokens: {errorContent}");
                }


                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                string accessToken = tokenResponse.access_token;
                string refreshToken = tokenResponse.refresh_token;
                int expiresIn = tokenResponse.expires_in;
                DateTime expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                var qboTokenResponse = new QuickBooksTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RealmId = realmId,
                    AccessTokenExpiry = expiresAt,
                    RefreshTokenExpiry = DateTime.UtcNow.AddDays(60)
                };

                string companyName = await GetQuickBooksCompanyNameAsync(accessToken, realmId);

                var connection = new ConnectionModal
                {
                    SourceAccounting = "QuickBooks",
                    ExternalId = realmId,
                    ExternalName = companyName,
                    TokenJson = JsonConvert.SerializeObject(qboTokenResponse),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastModifiedDate = new DateTime(2000, 1, 1)
                };

                var saveResult = await _connectionRepository.SaveConnectionAsync(connection);

                if (saveResult == null || saveResult.Status != 200)
                {
                    if (saveResult == null || saveResult.Status == 409)
                    {
                        var existingConnection = await _context.Connections
                            .FirstOrDefaultAsync(c => c.SourceAccounting == "QuickBooks" && c.ExternalId == realmId);

                        if (existingConnection != null)
                        {
                            existingConnection.ExternalName = companyName;
                            existingConnection.TokenJson = JsonConvert.SerializeObject(qboTokenResponse);
                            existingConnection.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Connection saved: {saveResult.Message}");
                    }
                }

                return new CommonResponse<QuickBooksTokenResponse>(200, "QuickBooks authorization successful", qboTokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during QuickBooks callback processing");
                return new CommonResponse<QuickBooksTokenResponse>(500, "An error occurred while processing the QuickBooks callback");
            }
        }


        private async Task<string> GetQuickBooksCompanyNameAsync(string accessToken, string realmId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var baseUrl = _configuration["QuickBookBaseUrl"];
                var response = await httpClient.GetAsync($"{baseUrl}{realmId}/companyinfo/{realmId}?minorversion=75");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get company info: {response.StatusCode}");
                    return "Unknown Company";
                }

                var content = await response.Content.ReadAsStringAsync();
                var companyInfo = JsonConvert.DeserializeObject<dynamic>(content);
                return companyInfo.CompanyInfo.CompanyName ?? "Unknown Company";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving QuickBooks company name");
                return "Unknown Company";
            }
        }


        public async Task<CommonResponse<XeroTokenResponse>> HandleXeroCallbackAsync(string code)
        {
            try
            {
                var clientId = _configuration["Xero:ClientId"];
                var clientSecret = _configuration["Xero:ClientSecret"];
                var redirectUri = _configuration["Xero:RedirectUri"];

                var client = _httpClientFactory.CreateClient();
                var requestBody = new StringContent(
                    $"grant_type=authorization_code&code={Uri.EscapeDataString(code)}&redirect_uri={Uri.EscapeDataString(redirectUri)}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");
                var tokenUrl = _configuration["Xero:TokenEndpoint"];
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = requestBody
                };

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
                tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = await client.SendAsync(tokenRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Xero token exchange failed: {errorContent}");
                    return new CommonResponse<XeroTokenResponse>((int)response.StatusCode, "Failed to exchange authorization code for tokens");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);

                var accessToken = tokenResponse.AccessToken;
                var refreshToken = tokenResponse.RefreshToken;
                var accessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiry);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(60);
                var connectionUri = _configuration["Xero:ConnectionPoint"];
                var connectionRequest = new HttpRequestMessage(HttpMethod.Get, connectionUri);
                connectionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var connectionResponse = await client.SendAsync(connectionRequest);

                if (!connectionResponse.IsSuccessStatusCode)
                {
                    var errorContent = await connectionResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Xero connections retrieval failed: {errorContent}");
                    return new CommonResponse<XeroTokenResponse>((int)connectionResponse.StatusCode, "Failed to retrieve Xero connections");
                }

                var connectionBody = await connectionResponse.Content.ReadAsStringAsync();
                var connections = JsonConvert.DeserializeObject<List<XeroConnection>>(connectionBody);

                var firstTenant = connections.FirstOrDefault();
                if (firstTenant == null)
                {
                    return new CommonResponse<XeroTokenResponse>(400, "No connected Xero tenant found");
                }

                var xeroTokenResponse = new XeroTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TenantId = firstTenant.TenantId,
                    TenantName = firstTenant.TenantName,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry
                };

                var connection = new ConnectionModal
                {
                    SourceAccounting = "Xero",
                    ExternalId = firstTenant.TenantId,
                    ExternalName = firstTenant.TenantName,
                    TokenJson = JsonConvert.SerializeObject(xeroTokenResponse),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastModifiedDate = new DateTime(2000, 1, 1)

                };

                var saveResult = await _connectionRepository.SaveConnectionAsync(connection);

                if (saveResult == null || saveResult.Status != 200)
                {
                    if (saveResult?.Status == 409)
                    {
                        var existingConnection = await _context.Connections
                            .FirstOrDefaultAsync(c => c.SourceAccounting == "Xero" && c.ExternalId == firstTenant.TenantId);

                        if (existingConnection != null)
                        {
                            existingConnection.ExternalName = firstTenant.TenantName;
                            existingConnection.TokenJson = JsonConvert.SerializeObject(xeroTokenResponse);
                            existingConnection.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Connection save failed: {saveResult.Message}");
                    }
                }

                return new CommonResponse<XeroTokenResponse>(200, "Xero authorization successful", xeroTokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Xero callback processing");
                return new CommonResponse<XeroTokenResponse>(500, "An error occurred while processing the Xero callback");
            }
        }



        //public async Task<bool> RevokeTokenAsync(string refreshToken)
        //{
        //    var clientId = _configuration["QuickBooks:ClientId"];
        //    var clientSecret = _configuration["QuickBooks:ClientSecret"];
        //    var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        //    var url = "https://developer.api.intuit.com/v2/oauth2/tokens/revoke";
        //    var request = new HttpRequestMessage(HttpMethod.Post, url);
        //    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

        //    var payload = new { token = refreshToken };
        //    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        //    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        //    var response = await _httpClient.SendAsync(request);
        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    return response.IsSuccessStatusCode;
        //}

        //public async Task<(string AccessToken, string RefreshToken)> RefreshQuickBooksTokenAsync(string oldRefreshToken,string realmId)
        //{
        //    var clientId = _configuration["QuickBooks:ClientId"];
        //    var clientSecret = _configuration["QuickBooks:ClientSecret"];
        //    var redirectUri = _configuration["QuickBooks:RedirectUri"];
        //    var tokenUrl = _configuration["QuickBookTokenUrl"];

        //    using var httpClient = new HttpClient();

        //    var requestBody = new Dictionary<string, string>
        //    {
        //            { "grant_type", "refresh_token" },
        //            { "refresh_token", oldRefreshToken },
        //            { "redirect_uri", redirectUri }
        //    };

        //    var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        //    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //    var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(requestBody));
        //    var content = await response.Content.ReadAsStringAsync();

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var tokenResult = JsonDocument.Parse(content).RootElement;
        //        var newAccessToken = tokenResult.GetProperty("access_token").GetString();
        //        var newRefreshToken = tokenResult.TryGetProperty("refresh_token", out var refreshElement)
        //            ? refreshElement.GetString()
        //            : oldRefreshToken;
        //        var accessTokenExpiresIn = tokenResult.GetProperty("expires_in").GetInt32();
        //        var accessTokenExpiryDate = DateTime.UtcNow.AddSeconds(accessTokenExpiresIn);
        //        var refreshTokenExpiryDate = tokenResult.TryGetProperty("x_refresh_token_expires_in", out var refreshExpiryElement)
        //            ? DateTime.UtcNow.AddSeconds(refreshExpiryElement.GetInt32())
        //            : DateTime.UtcNow.AddDays(90);
        //        var existingToken = await _context.QuickBooksTokens.FirstOrDefaultAsync(q => q.RealmId == realmId);
        //        if (existingToken != null)
        //        {
        //            existingToken.AccessToken = newAccessToken;
        //            existingToken.RefreshToken = newRefreshToken;
        //            existingToken.AccessTokenExpiry = accessTokenExpiryDate;
        //            existingToken.RefreshTokenExpiry = refreshTokenExpiryDate;
        //            _context.QuickBooksTokens.Update(existingToken);
        //        }
        //        await _context.SaveChangesAsync();
        //        return (newAccessToken, newRefreshToken);
        //    }
        //    Console.WriteLine("Token refresh failed: " + content);
        //    throw new UnauthorizedAccessException("Failed to refresh QuickBooks token.");
        //}



    }
}
