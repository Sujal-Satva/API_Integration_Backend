using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Models
{
    public class UnifiedVendor
    {
        public int Id { get; set; }
        public string? ExternalId { get; set; }      // changed
        public string? SourceSystem { get; set; }    // changed
        public string? DisplayName { get; set; }     // changed
        public string? CompanyName { get; set; }     // changed
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public bool Active { get; set; }
        public decimal? Balance { get; set; }
        public bool? Vendor1099 { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }


}
