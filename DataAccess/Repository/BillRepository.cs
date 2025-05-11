using DataAccess.Helper;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuickBookService.Interfaces;

using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using SharedModels.Xero.Models;
using System.Net.Http.Headers;
using task_14.Data;
using task_14.Models;
using task_14.Services;
using XeroService.Interfaces;
using XeroService.Services;

namespace task_14.Repository
{
    public class BillRepository:IBillRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionRepository _connectionRepository;
        private readonly IXeroBillService _xeroBillService;
        private readonly IQuickBooksBillService _quickBooksBillService;
        private readonly SyncingFunction _syncingFunction;


        public BillRepository(ApplicationDbContext context,IXeroBillService xeroBillService,SyncingFunction syncingFunction, IQuickBooksBillService quickBooksBillService, IConnectionRepository connectionRepository)
        {
            _context = context;
            _quickBooksBillService = quickBooksBillService;
            _connectionRepository = connectionRepository;
            _syncingFunction = syncingFunction;
            _xeroBillService = xeroBillService;
        }


        public async Task<CommonResponse<object>> SyncBills(string platform)
        {
            try
            {
                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                CommonResponse<object> response;

                switch (platform.ToLower())
                {
                    case "quickbooks":
                        response = await _quickBooksBillService.FetchBillsFromQuickBooks(connectionResult.Data);
                        break;

                    case "xero":
                        response = await _xeroBillService.FetchBillsFromXero(connectionResult.Data);
                        break;

                    default:
                        return new CommonResponse<object>(400, $"Unsupported platform: {platform}");
                }

                if (response.Data == null)
                    return new CommonResponse<object>(response.Status, response.Message);

                var unifiedItems = response.Data as List<UnifiedBill>;
                if (unifiedItems == null || !unifiedItems.Any())
                    return new CommonResponse<object>(400, "No bills found to sync");

                await _syncingFunction.UpdateSyncingInfo(connectionResult.Data.ExternalId, "Bills", DateTime.UtcNow);
                return await _syncingFunction.StoreUnifiedBillsAsync(unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Failed to sync bills", ex.Message);
            }
        }


        private static List<BillLineItem> DeserializeLineItems(string lineItemsJson)
        {
            if (string.IsNullOrWhiteSpace(lineItemsJson))
            {
                return new List<BillLineItem>(); 
            }

            try
            {
                return JsonConvert.DeserializeObject<List<BillLineItem>>(lineItemsJson);
            }
            catch (JsonException ex)
            {
                
                return new List<BillLineItem>();
            }
        }

            private static Contact DeserializeVendorDetails(string vendorDetailsJson)
            {
                if (string.IsNullOrWhiteSpace(vendorDetailsJson))
                {
                    return null;  
                }

                try
                {
                    return JsonConvert.DeserializeObject<Contact>(vendorDetailsJson);
                }
                catch (JsonException ex)
                { 
                    return null; 
                }
            }

        public async Task<CommonResponse<PagedResponse<object>>> GetBillsAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortColumn = null,
            string? sortDirection = "asc",
            bool pagination = true, 
            string sourceSystem = "all")
        {
            try
            {
                var query = _context.UnifiedBills.AsQueryable();

                query = query.Where(i => i.Status != "VOIDED" && i.Status != "DELETED");

                if (!string.IsNullOrEmpty(sourceSystem) && sourceSystem.ToLower() != "all")
                {
                    query = query.Where(b => b.SourceSystem == sourceSystem);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(b =>
                        b.VendorName.Contains(search) ||
                        b.ExternalId.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(sortColumn))
                {
                    bool descending = sortDirection?.ToLower() == "desc";

                    query = sortColumn.ToLower() switch
                    {
                        "vendorname" => descending ? query.OrderByDescending(b => b.VendorName) : query.OrderBy(b => b.VendorName),
                        "issuedate" => descending ? query.OrderByDescending(b => b.IssueDate) : query.OrderBy(b => b.IssueDate),
                        "duedate" => descending ? query.OrderByDescending(b => b.DueDate) : query.OrderBy(b => b.DueDate),
                        "totalamount" => descending ? query.OrderByDescending(b => b.TotalAmount) : query.OrderBy(b => b.TotalAmount),
                        "status" => descending ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
                        _ => query.OrderByDescending(b => b.UpdatedAt) 
                    };
                }
                

                var totalRecords = await query.CountAsync();

                if (pagination)
                {
                    query = query.Skip((page - 1) * pageSize).Take(pageSize);
                }

                var bills = await query.ToListAsync();
                var transformedBills = bills.Select(bill => new
                {
                    bill.Id,
                    bill.ExternalId,
                    bill.SourceSystem,
                    bill.VendorName,
                    bill.VendorId,
                    bill.IssueDate,
                    bill.DueDate,
                    bill.Currency,
                    bill.TotalAmount,
                    bill.Status,
                    bill.CreatedAt,
                    bill.UpdatedAt,
                    // Use DeserializeLineItems for line items
                    LineItems = DeserializeLineItems(bill.LineItems),
                    VendorDetails = DeserializeVendorDetails(bill.VendorDetails)
                }).ToList();

                var response = new PagedResponse<object>(
                    transformedBills,  // Use the transformed data here
                    page,
                    pageSize,
                    totalRecords
                );

                return new CommonResponse<PagedResponse<object>>(200, "Bills fetched successfully", response);
            }
            catch (Exception ex)
            {
                // Handle exception and return a response
                return new CommonResponse<PagedResponse<object>>(500, "Error fetching bills", null);
            }
        }

        public async Task<CommonResponse<object>> AddBillsAsync(string platform, object input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(platform))
                    return new CommonResponse<object>(400, "Platform is required");

                var connectionResult = await _connectionRepository.GetConnectionAsync(platform);
                if (connectionResult.Status != 200 || connectionResult.Data == null)
                    return new CommonResponse<object>(connectionResult.Status, connectionResult.Message);

                CommonResponse<object> response;

                switch (platform.ToLower())
                {
                    case "xero":
                        var xeroModel = JsonConvert.DeserializeObject<UnifiedInvoiceInputModel>(input.ToString());
                        response = await _xeroBillService.AddBill(connectionResult.Data, xeroModel);
                        break;

                    case "quickbooks":
                        var qbModel = JsonConvert.DeserializeObject<CreateBillRequest>(input.ToString());
                        response = await _quickBooksBillService.AddBillToQuickBooks(qbModel,connectionResult.Data);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedBill> bills && bills.FirstOrDefault() is UnifiedBill updatedBill)

                {
                    await _syncingFunction.StoreUnifiedBillsAsync(bills);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while adding the item", ex.Message);
            }
        }



        //public async Task<ApiResponse<string>> CreateBillAsync(QuickBooksBillCreateDto billDto, string token, string realmId)
        //{

        //    var payload = new
        //    {
        //        VendorRef = new { value = billDto.VendorRef.Value },
        //        APAccountRef = new { value = billDto.APAccountRef.Value },
        //        TxnDate = billDto.TxnDate,
        //        DueDate = billDto.DueDate,
        //        PrivateNote = billDto.PrivateNote,
        //        Line = billDto.Line.Select(line => new
        //        {
        //            Amount = line.Amount,
        //            DetailType = line.DetailType,
        //            Description = line.Description,
        //            AccountBasedExpenseLineDetail = line.AccountBasedExpenseLineDetail != null ? new
        //            {
        //                AccountRef = new { value = line.AccountBasedExpenseLineDetail.AccountRef.Value },
        //                CustomerRef = line.AccountBasedExpenseLineDetail.CustomerRef != null ? new { value = line.AccountBasedExpenseLineDetail.CustomerRef.Value } : null
        //            } : null,
        //            ItemBasedExpenseLineDetail = line.ItemBasedExpenseLineDetail != null ? new
        //            {
        //                ItemRef = new { value = line.ItemBasedExpenseLineDetail.ItemRef.Value },
        //                Qty = line.ItemBasedExpenseLineDetail.Qty,
        //                UnitPrice = line.ItemBasedExpenseLineDetail.UnitPrice,
        //                CustomerRef = line.ItemBasedExpenseLineDetail.CustomerRef != null ? new { value = line.ItemBasedExpenseLineDetail.CustomerRef.Value } : null,
        //                BillableStatus = line.ItemBasedExpenseLineDetail.BillableStatus
        //            } : null
        //        }).ToList(),
        //        TotalAmt = billDto.TotalAmt,
        //        CurrencyRef = new { value = billDto.CurrencyRef.Value }
        //    };


        //    var json = JsonConvert.SerializeObject(payload);

        //    var client = _httpClientFactory.CreateClient();
        //    var qburl = _configuration["QuickBookBaseUrl"];
        //    var request = new HttpRequestMessage(HttpMethod.Post, $"{qburl}{realmId}/bill?minorversion=65")
        //    {
        //        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        //    };
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //    var response = await client.SendAsync(request);

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        var error = await response.Content.ReadAsStringAsync();
        //        return new ApiResponse<string>
        //        {
        //            Message = "Failed to create bill in QuickBooks.",
        //            Error=error
        //        };
        //    }

        //    var content = await response.Content.ReadAsStringAsync();
        //    var jsonResponse = JObject.Parse(content);
        //    var qbBill = jsonResponse["Bill"]?.ToObject<QuickBooksBillDto>();

        //    if (qbBill == null)
        //    {
        //        return new ApiResponse<string>
        //        {
        //            Message = "QuickBooks returned an empty response.",
        //            Data = content
        //        };
        //    }
        //    var bill = new Bill
        //    {
        //        QuickBooksBillId = qbBill.Id,
        //        SyncToken = qbBill.SyncToken,
        //        TxnDate = qbBill.TxnDate,
        //        DueDate = qbBill.DueDate,
        //        TotalAmt = qbBill.TotalAmt,
        //        Balance = qbBill.Balance,
        //        PrivateNote = qbBill.PrivateNote,
        //        CurrencyValue = qbBill.Currency?.Value,
        //        CurrencyName = qbBill.Currency?.Name,
        //        VendorId = qbBill.VendorRef?.Value,
        //        APAccountId = qbBill.APAccountRef?.Value,
        //        CreateTime = qbBill.MetaData?.CreateTime,
        //        LastUpdatedTime = qbBill.MetaData?.LastUpdatedTime,
        //        BillLines = new List<BillLine>()
        //    };

        //    foreach (var line in qbBill.Line ?? new List<QuickBooksLineDto>())
        //    {
        //        var billLine = new BillLine
        //        {
        //            QuickBooksLineId = line.Id,
        //            LineNum = line.LineNum ?? 0,
        //            Description = line.Description,
        //            Amount = line.Amount,
        //            DetailType = line.DetailType,
        //            AccountId = line.AccountBasedExpenseLineDetail?.AccountRef?.Value,
        //            CustomerId = int.TryParse(line.AccountBasedExpenseLineDetail?.CustomerRef?.Value, out var cid) ? cid : 0,
        //            BillableStatus = line.ItemBasedExpenseLineDetail?.BillableStatus,
        //            ItemId = line.ItemBasedExpenseLineDetail?.ItemRef?.Value,
        //            Qty = line.ItemBasedExpenseLineDetail?.Qty,
        //            UnitPrice = line.ItemBasedExpenseLineDetail?.UnitPrice
        //        };

        //        bill.BillLines.Add(billLine);
        //    }
        //    _context.Bills.Add(bill);
        //    await _context.SaveChangesAsync();

        //    return new ApiResponse<string>
        //    {
        //        Message = "Bill successfully created in QuickBooks and saved locally.",
        //        Data = qbBill.Id,
        //        Error=null
        //    };
        //}

        //private Bill MapQuickBooksBillToEntity(QuickBooksBillDto qbBill)
        //{
        //    return new Bill
        //    {
        //        QuickBooksBillId = qbBill.Id,
        //        SyncToken = qbBill.SyncToken ?? "",
        //        TxnDate = qbBill.TxnDate ?? DateTime.MinValue,
        //        DueDate = qbBill.DueDate ?? DateTime.MinValue,
        //        TotalAmt = qbBill.TotalAmt,
        //        Balance = qbBill.Balance,
        //        PrivateNote = qbBill.PrivateNote ?? "",
        //        CurrencyValue = qbBill.Currency?.Value ?? "",
        //        CurrencyName = qbBill.Currency?.Name ?? "",
        //        VendorId = qbBill.VendorRef?.Value ?? "",
        //        APAccountId = qbBill.APAccountRef?.Value ?? "",
        //        CreateTime = qbBill.MetaData?.CreateTime ?? DateTime.MinValue,
        //        LastUpdatedTime = qbBill.MetaData?.LastUpdatedTime ?? DateTime.MinValue,
        //        BillLines = MapBillLines(qbBill.Line)
        //    };
        //}

        //private List<BillLine> MapBillLines(List<QuickBooksLineDto> lines)
        //{
        //    var billLines = new List<BillLine>();

        //    foreach (var line in lines)
        //    {
        //        billLines.Add(new BillLine
        //        {
        //            QuickBooksLineId = line.Id,
        //            LineNum = line.LineNum ?? 0,
        //            Description = line.Description ?? "",
        //            Amount = line.Amount,
        //            DetailType = line.DetailType ?? "",
        //            AccountId = line.AccountBasedExpenseLineDetail?.AccountRef?.Value ?? "",
        //            CustomerId = int.TryParse(line.AccountBasedExpenseLineDetail?.CustomerRef?.Value, out int customerId) ? customerId : 0,
        //            BillableStatus = line.ItemBasedExpenseLineDetail?.BillableStatus ?? "",
        //            ItemId = line.ItemBasedExpenseLineDetail?.ItemRef?.Value ?? "",
        //            Qty = line.ItemBasedExpenseLineDetail?.Qty ?? 0,
        //            UnitPrice = line.ItemBasedExpenseLineDetail?.UnitPrice ?? 0
        //        });
        //    }

        //    return billLines;
        //}

    }
}
