using Microsoft.Identity.Client;
using System.Data;
using System.Security.Principal;
using System;

namespace task_14.Models
{
    public class QBChartOfAccount
    {
        public string Id { get; set; }
        public string Name { get; set; }    
        public bool Active { get; set; }
        public string Classification { get; set; }
        public string AccountType { get; set; }
        public string Description { get; set; } 
        public string AccountSubType { get; set; }
        public double CurrentBalance { get; set; }
        public CurrencyRef CurrencyRef { get; set; }
        public refData MetaData {get; set;}
    }

    public class refData
    {
        public DateTime CreateTime { get; set; }

        public DateTime LastUpdatedTime { get; set; }
    } 

    public class CurrencyRef
    {
        public string value { get; set; }
        public string name { get; set; }
    }

    public class AccountQueryResponse
    {
        public List<QBChartOfAccount> Account { get; set; } = new List<QBChartOfAccount>();
    }

    public class QBRoot
    {
        public AccountQueryResponse QueryResponse { get; set; } = new AccountQueryResponse();
    }



    public class XeroAccountResponse
    {
        public List<XeroAccount> Accounts { get; set; }
    }

    public class XeroAccount
    {
        public string AccountId { get; set; }
        public string AccountID { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Class { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public double? Balance { get; set; }
        public string ReportingCode { get; set; }
        public string TaxType { get; set; }
        public DateTime? UpdatedDateUTC { get; set; }
    }

    public class ChartOfAccount
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Type { get; set; }

        public string? Classification { get; set; }

        public string? Description { get; set; }

        public bool Active { get; set; }

        public string? Currency { get; set; }

        public string? ExternalId { get; set; }

        public string? ReportingCode { get; set; }

        public decimal? CurrentBalance { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public string? SourceSystem { get; set; }

        public string? SourceSystemId { get; set; }
    }


    public class AccountViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; }
        public string AccountSubType { get; set; }
    }

    public class ProductAccountOptionsDto
    {
        public List<AccountDto> InventoryAssetAccounts { get; set; }
        public List<AccountDto> IncomeAccounts { get; set; }
        public List<AccountDto> ExpenseAccounts { get; set; }
    }

    public class AccountDto
    {
        public string QuickBooksAccountId { get; set; }
        public string Name { get; set; }
    }
}
