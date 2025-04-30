using Newtonsoft.Json;
namespace task_14.Models
{
    public class QuickBooksToken
    {
        public int Id { get; set; }
        public string RealmId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }

    public class XeroToken
    { 
        public int Id { get; set; }
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public string TenantId { get; set; }
        public string TenantName { get; set; }
    }


    public class XeroConnection
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("tenantName")]
        public string TenantName { get; set; }

        [JsonProperty("tenantType")]
        public string TenantType { get; set; }
    }


    public class QuickBooksTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string RealmId { get; set; }

        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }

    public class XeroTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TenantId { get; set; }
        public string TenantName { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int AccessTokenExpiry { get; set; }

        public DateTime RefreshTokenExpiry { get; set; }

    }
}
