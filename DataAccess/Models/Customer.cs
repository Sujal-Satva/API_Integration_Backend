using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace task_14.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? GivenName { get; set; }

        [MaxLength(50)]
        public string? FamilyName { get; set; }

        public string? CompanyName { get; set; }
        public string? Email { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? QBId { get; set; }
        public string? SyncToken { get; set; }
        public bool Active { get; set; } = true;

        public BillAddr? BillAddr { get; set; }
        public ShipAddr? ShipAddr { get; set; }
        public PrimaryPhone? PrimaryPhone { get; set; }
        public PrimaryEmailAddr? PrimaryEmailAddr { get; set; }
        public MetaData? MetaData { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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

    public class CustomerRoot
    {
        public CustomerQueryResponse QueryResponse { get; set; } = new CustomerQueryResponse();
    }

    public class CustomerInputModel
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? EmailAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? CountrySubDivisionCode { get; set; }
        public string? PostalCode { get; set; }

        public string? ExternalId { get; set; }
    }

        public class AllCustomer
        {
                public int Id { get; set; }
                public string? QuickBooksId { get; set; }
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

    public class CustomerDTO
    {
        public int Id { get; set; }
        public string? QuickBooksId { get; set; }
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

    public class QuickBooksAddCustomerResponse
            {
                public Customer Customer { get; set; }
            }
            public class XeroCustomerResponse
            {
                public List<XeroCustomer> Contacts { get; set; }
            }

            public class XeroAddress
            {
                public string AddressType { get; set; }
                public string AddressLine1 { get; set; }
                public string AddressLine2 { get; set; }
                public string City { get; set; }
                public string Region { get; set; }
                public string PostalCode { get; set; }
                public string Country { get; set; }
            }

            public class XeroPhone
            {
                public string PhoneType { get; set; }
                public string PhoneNumber { get; set; }
                public string PhoneAreaCode { get; set; }
                public string PhoneCountryCode { get; set; }
            }

            public class XeroCustomer
            {
                public string ContactID { get; set; }
                public string ContactStatus { get; set; }
                public string Name { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string EmailAddress { get; set; }
                public List<XeroAddress> Addresses { get; set; }
                public List<XeroPhone> Phones { get; set; }
                public string UpdatedDateUTC { get; set; }
                public List<string> ContactGroups { get; set; }
                public bool IsSupplier { get; set; }
                public bool IsCustomer { get; set; }
                public string Website { get; set; }
            }

}
