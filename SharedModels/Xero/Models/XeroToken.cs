using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Xero.Models
{
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

}
