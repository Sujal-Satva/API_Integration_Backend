using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using task_14.Data;
using task_14.Models;
using task_14.Services;

namespace task_14.Repository
{
    public class BillRepository:IBillRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BillRepository(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<ApiResponse<string>> SyncBillsFromQuickBooksAsync(string token, string realmId)
        {
            try
            {
                var qburl = _configuration["QuickBookBaseUrl"];
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{qburl}{realmId}/query?query=SELECT * FROM Bill");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<string>(error: errorContent, message: "Failed to fetch bills from QuickBooks.");
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var bills = json["QueryResponse"]?["Bill"]?.ToObject<List<QuickBooksBillDto>>();

                if (bills == null || !bills.Any())
                    return new ApiResponse<string>("No bills found to sync.");

                foreach (var qbBill in bills)
                {
                    var existingBill = await _context.Bills
                        .Include(b => b.BillLines)
                        .FirstOrDefaultAsync(b => b.QuickBooksBillId == qbBill.Id);

                    if (existingBill == null)
                    {
             
                        var newBill = MapQuickBooksBillToEntity(qbBill);
                        _context.Bills.Add(newBill);
                    }
                    else
                    {
                      
                        existingBill.SyncToken = qbBill.SyncToken ?? "";
                        existingBill.TxnDate = qbBill.TxnDate ?? DateTime.MinValue;
                        existingBill.DueDate = qbBill.DueDate ?? DateTime.MinValue;
                        existingBill.TotalAmt = qbBill.TotalAmt;
                        existingBill.Balance = qbBill.Balance;
                        existingBill.PrivateNote = qbBill.PrivateNote ?? "";
                        existingBill.CurrencyValue = qbBill.Currency?.Value ?? "";
                        existingBill.CurrencyName = qbBill.Currency?.Name ?? "";
                        existingBill.VendorId = qbBill.VendorRef?.Value ?? "";
                        existingBill.APAccountId = qbBill.APAccountRef?.Value ?? "";
                        existingBill.CreateTime = qbBill.MetaData?.CreateTime ?? DateTime.MinValue;
                        existingBill.LastUpdatedTime = qbBill.MetaData?.LastUpdatedTime ?? DateTime.MinValue;

                    
                        _context.BillLines.RemoveRange(existingBill.BillLines);
                        existingBill.BillLines = MapBillLines(qbBill.Line);
                    }
                }

                await _context.SaveChangesAsync();
                return new ApiResponse<string>("Bills synced successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(error: ex.Message, message: "An error occurred while syncing bills.");
            }
        }


        public async Task<PagedResponse<object>> GetBillsAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortColumn = null,
            string? sortDirection = "asc",
            bool pagination = true)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Bills
                .Include(b => b.BillLines)
                .AsQueryable();

            sortColumn = string.IsNullOrEmpty(sortColumn) ? "TxnDate" : sortColumn.ToLower();
            sortDirection = string.IsNullOrEmpty(sortDirection) ? "asc" : sortDirection.ToLower();

            query = sortColumn switch
            {
                "duedate" => sortDirection == "desc" ? query.OrderByDescending(b => b.DueDate) : query.OrderBy(b => b.DueDate),
                "totalamt" => sortDirection == "desc" ? query.OrderByDescending(b => b.TotalAmt) : query.OrderBy(b => b.TotalAmt),
                "balance" => sortDirection == "desc" ? query.OrderByDescending(b => b.Balance) : query.OrderBy(b => b.Balance),
                "currencyname" => sortDirection == "desc" ? query.OrderByDescending(b => b.CurrencyName) : query.OrderBy(b => b.CurrencyName),
                _ => sortDirection == "desc" ? query.OrderByDescending(b => b.TxnDate) : query.OrderBy(b => b.TxnDate),
            };

        
            var products = await _context.Products
                .GroupBy(p => p.QBItemId)
                .Select(g => g.First())
                .ToDictionaryAsync(p => p.QBItemId ?? "", p => p.Name ?? "");

            var vendors = await _context.Vendors
                .GroupBy(v => v.VId)
                .Select(g => g.First())
                .ToDictionaryAsync(v => v.VId ?? "", v => v.DisplayName ?? "");

            //var accounts = await _context.ChartOfAccounts
            //    .GroupBy(a => a.QBAccountId)
            //    .Select(g => g.First())
            //    .ToDictionaryAsync(a => a.QBAccountId ?? "", a => a.Name ?? "");

            var customers = await _context.Customers
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToDictionaryAsync(c => c.Id, c => c.DisplayName ?? "");

           
            var rawBills = await query.ToListAsync();

        
            var enrichedBills = rawBills.Select(b =>
            {
                vendors.TryGetValue(b.VendorId ?? "", out var vendorName);
                //accounts.TryGetValue(b.APAccountId ?? "", out var apAccountName);

                return new
                {
                    b.Id,
                    b.QuickBooksBillId,
                    b.TxnDate,
                    b.DueDate,
                    b.TotalAmt,
                    b.Balance,
                    b.PrivateNote,
                    b.CurrencyName,
                    b.CurrencyValue,
                    VendorName = vendorName,
                    //APAccountName = apAccountName,
                    b.CreateTime,
                    b.LastUpdatedTime,
                    BillLines = b.BillLines.Select(line =>
                    {
                        //accounts.TryGetValue(line.AccountId ?? "", out var accountName);
                        customers.TryGetValue(line.CustomerId, out var customerName);
                        products.TryGetValue(line.ItemId ?? "", out var productName);

                        return new
                        {
                            line.Id,
                            line.LineNum,
                            line.Description,
                            line.Amount,
                            line.DetailType,
                            //AccountName = accountName,
                            CustomerName = customerName,
                            ProductName = productName,
                            line.BillableStatus,
                            line.Qty,
                            line.UnitPrice
                        };
                    }).ToList()
                };
            }).ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                enrichedBills = enrichedBills.Where(b =>
                    (b.PrivateNote?.ToLower().Contains(search) ?? false) ||
                    (b.CurrencyName?.ToLower().Contains(search) ?? false) ||
                    (b.CurrencyValue?.ToString().ToLower().Contains(search) ?? false) ||
                    (b.VendorName?.ToLower().Contains(search) ?? false) ||
                    //(b.APAccountName?.ToLower().Contains(search) ?? false) ||
                    b.BillLines.Any(line =>
                        (line.Description?.ToLower().Contains(search) ?? false) ||
                        //(line.AccountName?.ToLower().Contains(search) ?? false) ||
                        (line.CustomerName?.ToLower().Contains(search) ?? false) ||
                        (line.ProductName?.ToLower().Contains(search) ?? false)
                    )
                ).ToList();
            }

            var filteredTotalRecords = enrichedBills.Count;
            if (pagination)
            {
                enrichedBills = enrichedBills.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }

            return new PagedResponse<object>(enrichedBills, page, pageSize, filteredTotalRecords);
        }



        public async Task<ApiResponse<string>> CreateBillAsync(QuickBooksBillCreateDto billDto, string token, string realmId)
        {

            var payload = new
            {
                VendorRef = new { value = billDto.VendorRef.Value },
                APAccountRef = new { value = billDto.APAccountRef.Value },
                TxnDate = billDto.TxnDate,
                DueDate = billDto.DueDate,
                PrivateNote = billDto.PrivateNote,
                Line = billDto.Line.Select(line => new
                {
                    Amount = line.Amount,
                    DetailType = line.DetailType,
                    Description = line.Description,
                    AccountBasedExpenseLineDetail = line.AccountBasedExpenseLineDetail != null ? new
                    {
                        AccountRef = new { value = line.AccountBasedExpenseLineDetail.AccountRef.Value },
                        CustomerRef = line.AccountBasedExpenseLineDetail.CustomerRef != null ? new { value = line.AccountBasedExpenseLineDetail.CustomerRef.Value } : null
                    } : null,
                    ItemBasedExpenseLineDetail = line.ItemBasedExpenseLineDetail != null ? new
                    {
                        ItemRef = new { value = line.ItemBasedExpenseLineDetail.ItemRef.Value },
                        Qty = line.ItemBasedExpenseLineDetail.Qty,
                        UnitPrice = line.ItemBasedExpenseLineDetail.UnitPrice,
                        CustomerRef = line.ItemBasedExpenseLineDetail.CustomerRef != null ? new { value = line.ItemBasedExpenseLineDetail.CustomerRef.Value } : null,
                        BillableStatus = line.ItemBasedExpenseLineDetail.BillableStatus
                    } : null
                }).ToList(),
                TotalAmt = billDto.TotalAmt,
                CurrencyRef = new { value = billDto.CurrencyRef.Value }
            };

           
            var json = JsonConvert.SerializeObject(payload);

            var client = _httpClientFactory.CreateClient();
            var qburl = _configuration["QuickBookBaseUrl"];
            var request = new HttpRequestMessage(HttpMethod.Post, $"{qburl}{realmId}/bill?minorversion=65")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new ApiResponse<string>
                {
                    Message = "Failed to create bill in QuickBooks.",
                    Error=error
                };
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(content);
            var qbBill = jsonResponse["Bill"]?.ToObject<QuickBooksBillDto>();

            if (qbBill == null)
            {
                return new ApiResponse<string>
                {
                    Message = "QuickBooks returned an empty response.",
                    Data = content
                };
            }
            var bill = new Bill
            {
                QuickBooksBillId = qbBill.Id,
                SyncToken = qbBill.SyncToken,
                TxnDate = qbBill.TxnDate,
                DueDate = qbBill.DueDate,
                TotalAmt = qbBill.TotalAmt,
                Balance = qbBill.Balance,
                PrivateNote = qbBill.PrivateNote,
                CurrencyValue = qbBill.Currency?.Value,
                CurrencyName = qbBill.Currency?.Name,
                VendorId = qbBill.VendorRef?.Value,
                APAccountId = qbBill.APAccountRef?.Value,
                CreateTime = qbBill.MetaData?.CreateTime,
                LastUpdatedTime = qbBill.MetaData?.LastUpdatedTime,
                BillLines = new List<BillLine>()
            };

            foreach (var line in qbBill.Line ?? new List<QuickBooksLineDto>())
            {
                var billLine = new BillLine
                {
                    QuickBooksLineId = line.Id,
                    LineNum = line.LineNum ?? 0,
                    Description = line.Description,
                    Amount = line.Amount,
                    DetailType = line.DetailType,
                    AccountId = line.AccountBasedExpenseLineDetail?.AccountRef?.Value,
                    CustomerId = int.TryParse(line.AccountBasedExpenseLineDetail?.CustomerRef?.Value, out var cid) ? cid : 0,
                    BillableStatus = line.ItemBasedExpenseLineDetail?.BillableStatus,
                    ItemId = line.ItemBasedExpenseLineDetail?.ItemRef?.Value,
                    Qty = line.ItemBasedExpenseLineDetail?.Qty,
                    UnitPrice = line.ItemBasedExpenseLineDetail?.UnitPrice
                };

                bill.BillLines.Add(billLine);
            }
            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Message = "Bill successfully created in QuickBooks and saved locally.",
                Data = qbBill.Id,
                Error=null
            };
        }

        private Bill MapQuickBooksBillToEntity(QuickBooksBillDto qbBill)
        {
            return new Bill
            {
                QuickBooksBillId = qbBill.Id,
                SyncToken = qbBill.SyncToken ?? "",
                TxnDate = qbBill.TxnDate ?? DateTime.MinValue,
                DueDate = qbBill.DueDate ?? DateTime.MinValue,
                TotalAmt = qbBill.TotalAmt,
                Balance = qbBill.Balance,
                PrivateNote = qbBill.PrivateNote ?? "",
                CurrencyValue = qbBill.Currency?.Value ?? "",
                CurrencyName = qbBill.Currency?.Name ?? "",
                VendorId = qbBill.VendorRef?.Value ?? "",
                APAccountId = qbBill.APAccountRef?.Value ?? "",
                CreateTime = qbBill.MetaData?.CreateTime ?? DateTime.MinValue,
                LastUpdatedTime = qbBill.MetaData?.LastUpdatedTime ?? DateTime.MinValue,
                BillLines = MapBillLines(qbBill.Line)
            };
        }

        private List<BillLine> MapBillLines(List<QuickBooksLineDto> lines)
        {
            var billLines = new List<BillLine>();

            foreach (var line in lines)
            {
                billLines.Add(new BillLine
                {
                    QuickBooksLineId = line.Id,
                    LineNum = line.LineNum ?? 0,
                    Description = line.Description ?? "",
                    Amount = line.Amount,
                    DetailType = line.DetailType ?? "",
                    AccountId = line.AccountBasedExpenseLineDetail?.AccountRef?.Value ?? "",
                    CustomerId = int.TryParse(line.AccountBasedExpenseLineDetail?.CustomerRef?.Value, out int customerId) ? customerId : 0,
                    BillableStatus = line.ItemBasedExpenseLineDetail?.BillableStatus ?? "",
                    ItemId = line.ItemBasedExpenseLineDetail?.ItemRef?.Value ?? "",
                    Qty = line.ItemBasedExpenseLineDetail?.Qty ?? 0,
                    UnitPrice = line.ItemBasedExpenseLineDetail?.UnitPrice ?? 0
                });
            }

            return billLines;
        }

    }
}
