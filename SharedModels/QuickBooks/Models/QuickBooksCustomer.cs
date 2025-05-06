using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.QuickBooks.Models
{
    public class QuickBookCustomer
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
        public List<QuickBookCustomer> Customer { get; set; } = new List<QuickBookCustomer>();
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
}
