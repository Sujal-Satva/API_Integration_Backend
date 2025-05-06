using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Models
{
    public class UnifiedCustomer
    {
        public int Id { get; set; }
        public string? QuickBooksId { get; set; }

        public string? ExternalId { get; set; }
        public string? XeroId { get; set; }
        public string SourceSystem { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Website { get; set; }
        public bool? Active { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? AddressType { get; set; }
        public string? PhoneType { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
    }
}
