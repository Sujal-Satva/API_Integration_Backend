

    using Newtonsoft.Json;

    namespace SharedModels.QuickBooks.Models
    {
        public class QuickBooksVendor
        {
            public string Id { get; set; }
            public string SyncToken { get; set; }
            public string DisplayName { get; set; }
            public string PrintOnCheckName { get; set; }
            public string GivenName { get; set; }
            public string FamilyName { get; set; }
            public string CompanyName { get; set; }
            public string AcctNum { get; set; }
            public decimal Balance { get; set; }
            public bool Vendor1099 { get; set; }
            public bool Active { get; set; }
            public string domain { get; set; }
            public bool sparse { get; set; }
            public string V4IDPseudonym { get; set; }

            public CurrencyRef CurrencyRef { get; set; }
            public MetaData MetaData { get; set; }
            public Address BillAddr { get; set; }

            public Phone PrimaryPhone { get; set; }
            public Phone Mobile { get; set; }
            public Phone Fax { get; set; }

            public Email PrimaryEmailAddr { get; set; }
            public WebAddr WebAddr { get; set; }
        }

        public class Address
        {
            public string Id { get; set; }
            public string Line1 { get; set; }
            public string City { get; set; }
            public string CountrySubDivisionCode { get; set; }
            public string PostalCode { get; set; }
            public string Lat { get; set; }
            public string Long { get; set; }
        }

        public class Phone
        {
            public string FreeFormNumber { get; set; }
        }

        public class Email
        {
            public string Address { get; set; }
        }

        public class WebAddr
        {
            public string URI { get; set; }
        }

        public class VendorQueryResponse
        {
            public List<QuickBooksVendor> Vendor { get; set; }
        }


        public class VendorRoot
        {
            public VendorQueryResponse QueryResponse { get; set; }
        }

        public class VendorInputModel
        {
            public string DisplayName { get; set; }
            public string? GivenName { get; set; }
            public string? FamilyName { get; set; }
            public string? CompanyName { get; set; }
            public string? EmailAddress { get; set; }
            public string? PhoneNumber { get; set; }
            public string? AddressLine1 { get; set; }
            public string? City { get; set; }
            public string? CountrySubDivisionCode { get; set; }
            public string? PostalCode { get; set; }
            
        }

}
