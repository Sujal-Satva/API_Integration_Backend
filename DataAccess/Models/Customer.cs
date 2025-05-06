using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace task_14.Models
{
    public class Customer
    {
        public int Id { get; set; }
        [Required]
     
        public string DisplayName { get; set; } = string.Empty;
     
        public string? GivenName { get; set; }
       
        public string? FamilyName { get; set; }

        public string? CompanyName { get; set; }
        public string? QBId { get; set; }
        public string? SyncToken { get; set; }
        public bool Active { get; set; } = true;
        public BillAddr? BillAddr { get; set; }
        public PrimaryPhone? PrimaryPhone { get; set; }
        public PrimaryEmailAddr? PrimaryEmailAddr { get; set; }
        public MetaData? MetaData { get; set; }
    }

    public class BillAddr
    {
        public string? Id { get; set; }
        public string? Line1 { get; set; }
        public string? City { get; set; }
        public string? CountrySubDivisionCode { get; set; }
        public string? PostalCode { get; set; }
    
    }

    public class ShipAddr
    {
        public string? Id { get; set; }
        public string? Line1 { get; set; }
        public string? City { get; set; }
        public string? CountrySubDivisionCode { get; set; }
        public string? PostalCode { get; set; }
    }

    public class PrimaryPhone
    {
        public string? Id { get; set; }
        public string? FreeFormNumber { get; set; }
    }

    public class PrimaryEmailAddr
    {
        public string? Id { get; set; }
        public string? Address { get; set; }
    }

    public class CustomerQueryResponse
    {
        public List<Customer> Customer { get; set; } = new List<Customer>();
    }

   
    public class CustomerDTO
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
        public string? PhoneNumber { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public int TotalCount { get; set; }
    }
}
