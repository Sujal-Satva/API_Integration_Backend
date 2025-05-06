using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels.Xero.Models
{
   
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
