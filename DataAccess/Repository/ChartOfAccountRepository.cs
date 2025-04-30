using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using task_14.Data;
using task_14.Models;
using task_14.Services;

public class ChartOfAccountRepository : IChartOfAccountRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ChartOfAccountRepository(ApplicationDbContext context, IHttpClientFactory httpClientFactory,IConfiguration configuration, IConnectionRepository connectionRepository)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _connectionRepository = connectionRepository;
    }

    private async Task<CommonResponse<object>> SyncChartOfAccountsAsync(string platform)
    {
        try
        {
            var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
            if (connectionResult.Status != 200 || connectionResult.Data == null)
                return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

            List<ChartOfAccount> accounts;
            if (platform == "QuickBooks")
            {
                var result = await FetchQuickBooksAccountsAsync(connectionResult.Data);
                if (result.Status != 200)
                    return new CommonResponse<object>(result.Status, result.Message);
                accounts = result.Data;
            }
            else if (platform == "Xero")
            {
                var result = await FetchXeroAccountsAsync(connectionResult.Data);
                if (result.Status != 200)
                    return new CommonResponse<object>(result.Status, result.Message);
                accounts = result.Data;
            }
            else
            {
                return new CommonResponse<object>(400, $"Unsupported platform: {platform}");
            }

            if (accounts == null || !accounts.Any())
                return new CommonResponse<object>(404, $"No accounts found in {platform}.");

            int added = 0, updated = 0;
            foreach (var account in accounts)
            {
                var existingAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a =>
                    a.SourceSystemId == account.SourceSystemId && a.SourceSystem == account.SourceSystem);

                if (existingAccount == null)
                {
                    _context.ChartOfAccounts.Add(account);
                    added++;
                }
                else
                {
                    existingAccount.Name = account.Name;
                    existingAccount.Type = account.Type;
                    existingAccount.Classification = account.Classification;
                    existingAccount.Description = account.Description;
                    existingAccount.Active = account.Active;
                    existingAccount.Currency = account.Currency;
                    existingAccount.ExternalId = account.ExternalId;
                    existingAccount.ReportingCode = account.ReportingCode;
                    existingAccount.CurrentBalance = account.CurrentBalance;
                    existingAccount.LastUpdated = account.LastUpdated;
                    existingAccount.SourceSystem = account.SourceSystem;
                    existingAccount.SourceSystemId = account.SourceSystemId;
                    updated++;
                }
            }

            await _context.SaveChangesAsync();
            return new CommonResponse<object>(200,
                $"Chart of Accounts synced from {platform} successfully. Added: {added}, Updated: {updated}");
        }
        catch (Exception ex)
        {
            
            return new CommonResponse<object>(500,
                $"An error occurred while syncing chart of accounts from {platform}");
        }
    }

    private async Task<CommonResponse<List<ChartOfAccount>>> FetchQuickBooksAccountsAsync(ConnectionModal connection)
    {
        try
        {
            var tokenJson = connection.TokenJson;
            var realmId = connection.ExternalId;
            var token = JsonConvert.DeserializeObject<QuickBooksToken>(tokenJson)?.AccessToken;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(realmId))
                return new CommonResponse<List<ChartOfAccount>>(401, "Invalid token or realm ID.");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var qbUrl = _configuration["QuickBookBaseUrl"];
            var url = $"{qbUrl}{realmId}/query?query=SELECT * FROM Account";

            var response = await httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new CommonResponse<List<ChartOfAccount>>((int)response.StatusCode,
                    "Error fetching accounts from QuickBooks.");
            }

            var result = JsonConvert.DeserializeObject<QBRoot>(json);
            if (result?.QueryResponse?.Account == null || !result.QueryResponse.Account.Any())
                return new CommonResponse<List<ChartOfAccount>>(404, "No accounts found in QuickBooks.");

            
            var accounts = result.QueryResponse.Account.Select(acc => new ChartOfAccount
            {
                ExternalId = acc.Id,
                Name = acc.Name,
                Type = acc.AccountType,
                Classification = acc.Classification,
                Description = acc.Description,
                Active = acc.Active,
                Currency = acc.CurrencyRef?.value,
                ReportingCode = acc.AccountSubType,
                CurrentBalance = (decimal)acc.CurrentBalance,
                LastUpdated = acc.MetaData?.LastUpdatedTime,
                SourceSystem = "QuickBooks",
                SourceSystemId = acc.Id
            }).ToList();

            return new CommonResponse<List<ChartOfAccount>>(200, "QuickBooks accounts fetched successfully", accounts);
        }
        catch (Exception ex)
        {
            return new CommonResponse<List<ChartOfAccount>>(500,
                $"Exception during QuickBooks account synchronization: {ex.Message}");
        }
    }


    private async Task<CommonResponse<List<ChartOfAccount>>> FetchXeroAccountsAsync(ConnectionModal connection)
    {
        try
        {
            var tokenJson = connection.TokenJson;
            var tenantId = connection.ExternalId;
            var token = JsonConvert.DeserializeObject<XeroTokenResponse>(tokenJson)?.AccessToken;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tenantId))
                return new CommonResponse<List<ChartOfAccount>>(401, "Invalid token or tenant ID.");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = "https://api.xero.com/api.xro/2.0/Accounts";
            var response = await httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new CommonResponse<List<ChartOfAccount>>((int)response.StatusCode,
                    "Error fetching accounts from Xero.");
            }

            var xeroResult = JsonConvert.DeserializeObject<XeroAccountResponse>(json);
            if (xeroResult?.Accounts == null || !xeroResult.Accounts.Any())
                return new CommonResponse<List<ChartOfAccount>>(404, "No accounts found in Xero.");

        
            var accounts = xeroResult.Accounts.Select(acc => new ChartOfAccount
            {
                ExternalId = acc.AccountID, 
                Name = acc.Name,
                Type = acc.Type,
                Active =  acc.Status == "ACTIVE",
                Classification = acc.Class,
                Description = acc.Description,
                Currency = "",
                ReportingCode = acc.ReportingCode,
                CurrentBalance = 0,
                LastUpdated = acc.UpdatedDateUTC,
                SourceSystem = "Xero",
                SourceSystemId = acc.AccountID
            }).ToList();

            return new CommonResponse<List<ChartOfAccount>>(200, "Xero accounts fetched successfully", accounts);
        }
        catch (Exception ex)
        {
            return new CommonResponse<List<ChartOfAccount>>(500,
                $"Exception during Xero account synchronization: {ex.Message}");
        }
    }


    public async Task<CommonResponse<object>> FetchAndSaveQBOChartOfAccountsAsync()
    {
        return await SyncChartOfAccountsAsync("QuickBooks");
    }

    public async Task<CommonResponse<object>> FetchAndSaveXeroChartOfAccountsAsync()
    {
        return await SyncChartOfAccountsAsync("Xero");
    }



    private async Task<List<AccountViewModel>> GetAccountsByTypeAsync(string realmId, string accountType, string token)
    {
        var qburl = _configuration["QuickBookBaseUrl"];
        var url = $"{qburl}{realmId}/query?query=select * from Account where AccountType = '{accountType}'";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(url);
        var quickBooksResponse = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            dynamic responseData = JsonConvert.DeserializeObject(quickBooksResponse);
            var accounts = new List<AccountViewModel>();

            foreach (var account in responseData.QueryResponse.Account)
            {
                accounts.Add(new AccountViewModel
                {
                    Id = account.Id,
                    Name = account.Name,
                    AccountType = account.AccountType,
                    AccountSubType = account.AccountSubType
                });
            }

            return accounts;
        }
        else
        {
            throw new Exception($"Error fetching {accountType} accounts: {quickBooksResponse}");
        }
    }

    public async Task<List<AccountViewModel>> GetIncomeAccountsAsync(string realmId, string token)
    {
        return await GetAccountsByTypeAsync(realmId, "Income", token);
    }

    public async Task<List<AccountViewModel>> GetExpenseAccountsAsync(string realmId, string token)
    {   
        return await GetAccountsByTypeAsync(realmId, "Expense", token);
    }

    public async Task<CommonResponse<PagedResponse<ChartOfAccount>>> GetAccountsFromDbAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortColumn = "Name",
            string? sortDirection = "asc",
            bool pagination = true,
            string? sourceSystem = null) 
    {
        try
        {
            var query = _context.ChartOfAccounts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(sourceSystem))
            {
                query = query.Where(a => a.SourceSystem.ToLower() == sourceSystem.ToLower());
            }

            // Search across multiple fields
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.Name.Contains(search) ||
                    a.Type.Contains(search) ||
                    a.Classification.Contains(search) ||
                    a.Description.Contains(search) ||
                    a.Currency.Contains(search) ||
                    a.ReportingCode.Contains(search));
            }

            // Sorting logic
            query = sortColumn?.ToLower() switch
            {
                "name" => sortDirection == "desc" ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
                "type" => sortDirection == "desc" ? query.OrderByDescending(a => a.Type) : query.OrderBy(a => a.Type),
                "classification" => sortDirection == "desc" ? query.OrderByDescending(a => a.Classification) : query.OrderBy(a => a.Classification),
                "description" => sortDirection == "desc" ? query.OrderByDescending(a => a.Description) : query.OrderBy(a => a.Description),
                "currency" => sortDirection == "desc" ? query.OrderByDescending(a => a.Currency) : query.OrderBy(a => a.Currency),
                "reportingcode" => sortDirection == "desc" ? query.OrderByDescending(a => a.ReportingCode) : query.OrderBy(a => a.ReportingCode),
                "currentbalance" => sortDirection == "desc" ? query.OrderByDescending(a => a.CurrentBalance) : query.OrderBy(a => a.CurrentBalance),
                "lastupdated" => sortDirection == "desc" ? query.OrderByDescending(a => a.LastUpdated) : query.OrderBy(a => a.LastUpdated),
                _ => query.OrderBy(a => a.Name)
            };

            var totalRecords = await query.CountAsync();

            if (!pagination)
            {
                var allData = await query.ToListAsync();
                var fullResponse = new PagedResponse<ChartOfAccount>(allData, 1, totalRecords, totalRecords);

                return new CommonResponse<PagedResponse<ChartOfAccount>>(200, "Accounts fetched successfully", fullResponse);
            }

            var pagedData = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<ChartOfAccount>(pagedData, page, pageSize, totalRecords);

            return new CommonResponse<PagedResponse<ChartOfAccount>>(200, "Accounts fetched successfully", response);
        }
        catch (Exception ex)
        {
            return new CommonResponse<PagedResponse<ChartOfAccount>>(500, "Error fetching accounts from database", null);
        }
    }

    public async Task<ProductAccountOptionsDto> GetProductAccountOptionsAsync(int? productId = null)
    {
        var accounts = await _context.ChartOfAccounts.ToListAsync();

        //var inventoryAssetAccounts = accounts
        //    .Where(a => a.AccountType == "Other Current Asset" &&
        //                (a.AccountSubType ?? "").Trim().Equals("Inventory", StringComparison.OrdinalIgnoreCase))
        //    .ToList();

        //var incomeAccounts = accounts
        //    .Where(a => a.AccountType == "Income" &&
        //                (a.AccountSubType ?? "").Trim().Equals("SalesOfProductIncome", StringComparison.OrdinalIgnoreCase))
        //    .ToList();

        //var expenseAccounts = accounts
        //    .Where(a => a.AccountType == "Cost of Goods Sold" &&
        //                (a.AccountSubType ?? "").Trim().Equals("SuppliesMaterialsCogs", StringComparison.OrdinalIgnoreCase))
        //    .ToList();


        return new ProductAccountOptionsDto
        {
            //InventoryAssetAccounts = inventoryAssetAccounts
            //    .Select(a => new AccountDto
            //    {
            //        QuickBooksAccountId = a.QBAccountId,
            //        Name = a.Name
            //    }).ToList(),

            //IncomeAccounts = incomeAccounts
            //    .Select(a => new AccountDto
            //    {
            //        QuickBooksAccountId = a.QBAccountId,
            //        Name = a.Name
            //    }).ToList(),

            //ExpenseAccounts = expenseAccounts
            //    .Select(a => new AccountDto
            //    {
            //        QuickBooksAccountId = a.QBAccountId,
            //        Name = a.Name
            //    }).ToList()
        };
    }
}
