using Microsoft.Extensions.Configuration;
using QuickBookService.Interfaces;
using SharedModels.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using SharedModels.QuickBooks.Models;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace QuickBookService.Services
{
    public class QuickBooksApiService : IQuickBooksApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
       
        private readonly string qbUrl;

        public QuickBooksApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
           
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            qbUrl = _configuration["QuickBookBaseUrl"];
        }

        public async Task<CommonResponse<object>> QuickBooksGetRequest(string endpoint, ConnectionModal connection)
        {
            var token = connection.TokenJson;
            var realmId = connection.ExternalId;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(realmId))
                return new CommonResponse<object>(400, "Invalid token or realm ID");
            var tokenResponse = JsonConvert.DeserializeObject<QuickBooksToken>(token)?.AccessToken;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{qbUrl}{realmId}/{endpoint}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<object>(content);
                return new CommonResponse<object>(200, "Success", result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new CommonResponse<object>((int)response.StatusCode, "Error", errorContent);
            }
        }

            public async Task<CommonResponse<object>> QuickBooksPostRequest(string endpoint, object payload, ConnectionModal connection)
            {
                var tokenJson = connection.TokenJson;
                var realmId = connection.ExternalId;

                if (string.IsNullOrEmpty(tokenJson) || string.IsNullOrEmpty(realmId))
                    return new CommonResponse<object>(400, "Invalid token or realm ID");

                var accessToken = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;
                if (string.IsNullOrEmpty(accessToken))
                    return new CommonResponse<object>(401, "Access token is missing or invalid");

                try
                {
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    string[] endpointParts = endpoint.Split('?', 2);
                    string endpointPath = endpointParts[0];
                    string queryParams = endpointParts.Length > 1 ? endpointParts[1] : "";
                    var apiUrl = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/{endpointPath}";
                    var queryString = $"?minorversion=75";
                    if (!string.IsNullOrEmpty(queryParams))
                    {
                        queryString += $"&{queryParams}";
                    }
                    apiUrl += queryString;
                    var response = await client.PostAsync(apiUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<object>(responseBody);
                        return new CommonResponse<object>(200, "Success", result);
                    }

                    return new CommonResponse<object>((int)response.StatusCode, "Error", responseBody);
                }
                catch (Exception ex)
                {
                    return new CommonResponse<object>(500, "Unexpected error", ex.Message);
                }
            }

    }
}
