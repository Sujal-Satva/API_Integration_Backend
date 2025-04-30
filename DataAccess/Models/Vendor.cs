namespace task_14.Models
{
    public class Vendor
    {
        public int Id { get; set; }                    // Unique Vendor ID (Database Identity)
        public string VId { get; set; }                // Vendor ID from QuickBooks (e.g., "56")
        public string DisplayName { get; set; }        // Vendor's display name
        public bool Active { get; set; }               // Vendor active status (true/false)
        public bool Vendor1099 { get; set; }           // Whether the vendor is 1099 eligible (true/false)
        public decimal Balance { get; set; }           // Vendor balance

        public string CurrencyValue { get; set; }      // Currency value (e.g., "USD")
        public string CurrencyName { get; set; }       // Currency name (e.g., "United States Dollar")

        public string BillAddrLine1 { get; set; }      // Bill address line 1
        public string BillAddrCity { get; set; }       // Bill address city
        public string BillAddrPostalCode { get; set; } // Bill address postal code

        public string PrimaryPhone { get; set; }       // Vendor's primary phone number
        public string PrimaryEmailAddr { get; set; }   // Vendor's primary email address
        public string WebAddr { get; set; }            // Vendor's website URL

        public string SyncToken { get; set; }         
        public string V4IDPseudonym { get; set; }      
        public DateTime CreateTime { get; set; }      
        public DateTime LastUpdatedTime { get; set; } 

        public string GivenName { get; set; }          
        public string FamilyName { get; set; }         
        public string CompanyName { get; set; }        
    }


    public class VendorInputModal
    {


        public string DisplayName { get; set; }       
        public decimal Balance { get; set; }         
        public string CurrencyValue { get; set; }    
        public string CurrencyName { get; set; }     
        public bool Active { get; set; }
        public string? BillAddrLine1 { get; set; }      
        public string? BillAddrCity { get; set; }      
        public string? BillAddrPostalCode { get; set; }
        public string? PrimaryPhone { get; set; }      
        public string? PrimaryEmailAddr { get; set; }  
        public string? WebAddr { get; set; }
        public string? GivenName { get; set; }         
        public string? FamilyName { get; set; }         
        public string? CompanyName { get; set; }   
    }
}
