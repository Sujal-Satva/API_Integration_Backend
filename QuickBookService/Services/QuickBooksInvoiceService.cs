using Newtonsoft.Json;
using QuickBookService.Interfaces;
using QuickBookService.Mappers;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QuickBookService.Services.QuickBooksProductService;

namespace QuickBookService.Services
{
    public class QuickBooksInvoiceService:IQuickBooksInvoiceServices
    {
        private readonly IQuickBooksApiService _quickBooksApiService;
        public QuickBooksInvoiceService(IQuickBooksApiService quickBooksApiService)
        {
            _quickBooksApiService = quickBooksApiService;
        }

        private DateTime GetLastSyncedDate(string syncingInfoJson, string key)
        {
            if (string.IsNullOrEmpty(syncingInfoJson)) return new DateTime(2000, 1, 1);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(syncingInfoJson);
            if (dict != null && dict.TryGetValue(key, out var value))
            {
                if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    return parsed;
                }
            }
            return new DateTime(2000, 1, 1);
        }

        public async Task<CommonResponse<object>> FetchInvoicesFromQuickBooks(ConnectionModal connection)
        {
            try
            {
                int startPosition = 1;
                const int maxResults = 1000;
                var lastSync = GetLastSyncedDate(connection.SyncingInfo, "Invoices");

                var allInvoices = new List<QuickBooksInvoice>();

                while (true)
                {
                    string query = $"SELECT * FROM Invoice WHERE Metadata.LastUpdatedTime > '{lastSync:yyyy-MM-ddTHH:mm:ssZ}' " +
                                   $"STARTPOSITION {startPosition} MAXRESULTS {maxResults}";
                    var apiResponse = await _quickBooksApiService.QuickBooksGetRequest(
                        $"query?query={Uri.EscapeDataString(query)}", connection);

                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch invoices", apiResponse.Data);

                    var result = JsonConvert.DeserializeObject<InvoiceRoot>(apiResponse.Data.ToString());
                    var invoices = result?.QueryResponse?.Invoice;

                    if (invoices == null || invoices.Count == 0)
                        break;

                    allInvoices.AddRange(invoices);

                    if (invoices.Count < maxResults)
                        break;

                    startPosition += maxResults;
                }

                if (allInvoices.Count == 0)
                    return new CommonResponse<object>(200, "All invoices are already synced", null);
                var unifiedInvoices = new List<UnifiedInvoice>();
                foreach (var invoice in allInvoices)
                {
                    try
                    {
                        var mapped = InvoiceMapper.MapQuickBooksInvoiceToUnified(invoice);
                        if (mapped != null)
                        {
                            unifiedInvoices.Add(mapped);
                        }
                    }
                    catch (Exception mapEx)
                    { 
                        Console.WriteLine($"Error mapping invoice ID {invoice?.Id}: {mapEx.Message}");
                        continue;
                    }
                }

                return new CommonResponse<object>(200, "Invoices fetched and mapped successfully", unifiedInvoices);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing invoices", ex.Message);
            }
        }




        public async Task<CommonResponse<object>> AddInvoice(ConnectionModal connection, UnifiedInvoiceInputModel model)
        {
            try
            {
               
                var qbInvoicePayload = new
                {
                    CustomerRef = new { value = model.CustomerId },
                    TxnDate = model.InvoiceDate.ToString("yyyy-MM-dd"),
                    DueDate = model.DueDate.ToString("yyyy-MM-dd"),
                    BillAddr = model.Addresses?.FirstOrDefault() != null
                        ? new { Line1 = model.Addresses[0].Line1 }
                        : null,
                    PrivateNote = model.Reference,
                    Line = model.LineItems.Select(li => new
                    {
                        DetailType = "SalesItemLineDetail",
                        Amount = li.Quantity * li.Rate,
                        Description = li.Description,
                        SalesItemLineDetail = new
                        {
                            ItemRef = new { value = li.ProductId },
                            Qty = li.Quantity,
                            UnitPrice = li.Rate
                        }
                    }).ToList()
                };
                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("invoice", qbInvoicePayload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                   
                    var invoice = JsonConvert.DeserializeObject<QuickBooksInvoiceResponse>(apiResponse.Data.ToString())?.Invoice;
                    if (invoice != null)
                    {
                        var mapped = InvoiceMapper.MapQuickBooksInvoiceToUnified(invoice);
                        return new CommonResponse<object>(200, "Invoice created successfully", new List<UnifiedInvoice> { mapped });
                    }
                }
                return new CommonResponse<object>(apiResponse.Status, "Failed to create invoice", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while creating invoice", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> GetItemById(ConnectionModal connection, string itemId)
        {
            try
            {
                var apiResponse = await _quickBooksApiService.QuickBooksGetRequest($"invoice/{itemId}", connection);

                if (apiResponse.Status == 200)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksInvoiceResponse>(apiResponse.Data.ToString());
                    return new CommonResponse<object>(200, "invoice fetched successfully", result.Invoice);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to fetch invoice", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while fetching invoice", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditInvoice(ConnectionModal connection, string invoiceId, UnifiedInvoiceInputModel model)
        {
            try
            {
                var getResponse = await GetItemById(connection, invoiceId);
                if (getResponse.Status != 200)
                {
                    return new CommonResponse<object>(getResponse.Status, "Failed to retrieve current invoice for update", getResponse.Data);
                }

                var existingInvoice = JsonConvert.DeserializeObject<QuickBooksInvoice>(JsonConvert.SerializeObject(getResponse.Data));
                if (existingInvoice == null)
                {
                    return new CommonResponse<object>(404, "Invoice not found in QuickBooks", null);
                }
                var qbInvoicePayload = new
                {
                    Id = invoiceId,
                    SyncToken = existingInvoice.SyncToken,
                    CustomerRef = new { value = model.CustomerId },
                    TxnDate = model.InvoiceDate.ToString("yyyy-MM-dd"),
                    DueDate = model.DueDate.ToString("yyyy-MM-dd"),
                    BillAddr = model.Addresses?.FirstOrDefault() != null
                        ? new { Line1 = model.Addresses[0].Line1 }
                        : null,
                    PrivateNote = model.Reference,
                    Line = model.LineItems.Select(li => new
                    {
                        DetailType = "SalesItemLineDetail",
                        Amount = li.Quantity * li.Rate,
                        Description = li.Description,
                        SalesItemLineDetail = new
                        {
                            ItemRef = new { value = li.ProductId },
                            Qty = li.Quantity,
                            UnitPrice = li.Rate
                        }
                    }).ToList()
                };
                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("invoice?operation=update", qbInvoicePayload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var invoice = JsonConvert.DeserializeObject<QuickBooksInvoiceResponse>(apiResponse.Data.ToString())?.Invoice;
                    if (invoice != null)
                    {
                        var mapped = InvoiceMapper.MapQuickBooksInvoiceToUnified(invoice);
                        return new CommonResponse<object>(200, "Invoice updated successfully", new List<UnifiedInvoice> { mapped });
                    }
                }
                return new CommonResponse<object>(apiResponse.Status, "Failed to update invoice", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating invoice", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> DeleteInvoice(ConnectionModal connection, string invoiceId)
        {
            try
            {
                
                var getResponse = await GetItemById(connection, invoiceId);
                if (getResponse.Status != 200)
                {
                    return new CommonResponse<object>(getResponse.Status, "Failed to retrieve invoice for deletion", getResponse.Data);
                }

                var existingInvoice = JsonConvert.DeserializeObject<QuickBooksInvoice>(JsonConvert.SerializeObject(getResponse.Data));
                if (existingInvoice == null)
                {
                    return new CommonResponse<object>(404, "Invoice not found in QuickBooks", null);
                }
                var deletePayload = new
                {
                    Id = invoiceId,
                    SyncToken = existingInvoice.SyncToken,
                    sparse=true
                };
                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("invoice?operation=void", deletePayload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var invoice = JsonConvert.DeserializeObject<QuickBooksInvoiceResponse>(apiResponse.Data.ToString())?.Invoice;
                    if (invoice != null)
                    {
                        var mapped = InvoiceMapper.MapQuickBooksInvoiceToUnified(invoice);
                        return new CommonResponse<object>(200, "Invoice deleted successfully (soft delete)", mapped);
                    }
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to delete invoice", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while deleting invoice", ex.Message);
            }
        }


        public class QuickBooksInvoiceResponse
        {
            public QuickBooksInvoice Invoice { get; set; }
        }
    }
}
