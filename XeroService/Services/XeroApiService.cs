using Microsoft.Extensions.Configuration;
using XeroService.Interfaces;
using SharedModels.Models;
using System.Net.Http.Headers;
using SharedModels.Xero.Models;
using Newtonsoft.Json;

namespace XeroService.Services
{
    public class XeroApiService : IXeroApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string xeroUrl;

        public XeroApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        
            xeroUrl = _configuration["Xero:BaseUrl"];  
        }

        
        public async Task<CommonResponse<object>> XeroGetRequest(string endpoint, ConnectionModal connection)
        {
            var token = connection.TokenJson;
            var tenantId = connection.ExternalId;  

           
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tenantId))
                return new CommonResponse<object>(400, "Invalid token or tenant ID");

            var tokenResponse = JsonConvert.DeserializeObject<XeroToken>(token)?.AccessToken;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Xero-Tenant-Id", tenantId);  // Add Tenant ID to the request header

            var response = await client.GetAsync($"{xeroUrl}{endpoint}");

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


        public async Task<CommonResponse<object>> XeroPostRequest(string endpoint, object payload, ConnectionModal connection)
        {
            var token = connection.TokenJson;
            var tenantId = connection.ExternalId;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tenantId))
                return new CommonResponse<object>(400, "Invalid token or tenant ID");

            var tokenResponse = JsonConvert.DeserializeObject<XeroToken>(token)?.AccessToken;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Xero-Tenant-Id", tenantId); 

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{xeroUrl}{endpoint}", content);

            if (response.IsSuccessStatusCode)
            {
                var resultContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<object>(resultContent);
                return new CommonResponse<object>(200, "Success", result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new CommonResponse<object>((int)response.StatusCode, "Error", errorContent);
            }
        }
        public async Task<CommonResponse<object>> XeroDeleteRequest(string endpoint, ConnectionModal connection)
        {
            var token = connection.TokenJson;
            var tenantId = connection.ExternalId;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tenantId))
                return new CommonResponse<object>(400, "Invalid token or tenant ID");

            var accessToken = JsonConvert.DeserializeObject<XeroToken>(token)?.AccessToken;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Xero-Tenant-Id", tenantId);

            var response = await client.DeleteAsync($"{xeroUrl}{endpoint}");

            if (response.IsSuccessStatusCode)
            {
                var resultContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<object>(resultContent);
                return new CommonResponse<object>(200, "Success", result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new CommonResponse<object>((int)response.StatusCode, "Error", errorContent);
            }
        }


        //public async Task<CommonResponse<object>> XeroPutRequest(string endpoint, object payload, ConnectionModal connection)
        //{
        //    var token = connection.TokenJson;
        //    var tenantId = connection.ExternalId;

        //    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tenantId))
        //        return new CommonResponse<object>(400, "Invalid token or tenant ID");

        //    var tokenResponse = JsonConvert.DeserializeObject<XeroToken>(token)?.AccessToken;
        //    var client = _httpClientFactory.CreateClient();
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //    client.DefaultRequestHeaders.Add("Xero-Tenant-Id", tenantId);

        //    var jsonPayload = JsonConvert.SerializeObject(payload);
        //    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        //    var response = await client.PutAsync($"{xeroUrl}{endpoint}", content);

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var resultContent = await response.Content.ReadAsStringAsync();
        //        var result = JsonConvert.DeserializeObject<object>(resultContent);
        //        return new CommonResponse<object>(200, "Success", result);
        //    }
        //    else
        //    {
        //        var errorContent = await response.Content.ReadAsStringAsync();
        //        return new CommonResponse<object>((int)response.StatusCode, "Error", errorContent);
        //    }
        //}
    }


}
