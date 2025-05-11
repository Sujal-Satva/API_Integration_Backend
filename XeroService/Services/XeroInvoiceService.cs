using Newtonsoft.Json;
using SharedModels.Models;
using SharedModels.Xero.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeroService.Mappers;
using XeroService.Interfaces;

namespace XeroService.Services
{
    public class XeroInvoiceService:IXeroInvoiceService
    {
        private readonly IXeroApiService _xeroApiService;

        public XeroInvoiceService(IXeroApiService xeroApiService)
        {
            _xeroApiService = xeroApiService;
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

        public async Task<CommonResponse<object>> FetchInvoicesFromXero(ConnectionModal connection)
        {
            try
            {
                DateTime lastSynced = GetLastSyncedDate(connection.SyncingInfo, "Invoices");
                DateTime bufferedTime = lastSynced.ToUniversalTime().AddMinutes(-10);
                string baseFilter = $"";
                if (bufferedTime > new DateTime(2000, 1, 1))
                {
                    baseFilter += $"UpdatedDateUTC>=DateTime({bufferedTime:yyyy, MM, dd, HH, mm, ss})";
                }
                int page = 1;
                var allProducts = new List<XeroInvoice>();

                while (true)
                {
                    string url = $"/Invoices?page={page}&where={Uri.EscapeDataString(baseFilter)}";
                    var apiResponse = await _xeroApiService.XeroGetRequest($"{url}", connection);
                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch customers from Xero", apiResponse.Data);
                    var final = JsonConvert.DeserializeObject<XeroInvoiceResponse>(apiResponse.Data.ToString());
                    var invoices = final?.Invoices;
                    if (invoices == null || invoices.Count == 0) break;
                    allProducts.AddRange(invoices);
                    if (invoices.Count < 100)
                        break;
                    page++;
                }
                if (allProducts.Count == 0)
                    return new CommonResponse<object>(200, "All invoices are already Synced", null);
                var unifiedInvoices= new List<UnifiedInvoice>();
                foreach (var invoices in allProducts)
                {
                    unifiedInvoices.Add(InvoiceMapper.MapXeroInvoiceToUnified(invoices));
                }
                return new CommonResponse<object>(200, "invoices fetched and mapped successfully", unifiedInvoices);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing invoices from Xero", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> AddInvoice(ConnectionModal connection, UnifiedInvoiceInputModel input)
        {
            try
            {
               
                var invoicePayload = new
                {
                    Type = "ACCREC", 
                    Contact = new { Id = input.CustomerId, Name=input.CustomerName }, 
                    Date = input.InvoiceDate.ToString("yyyy-MM-dd"), // Invoice date
                    DueDate = input.DueDate.ToString("yyyy-MM-dd"), // Due date
                    LineItems = input.LineItems.Select(li => new
                    {
                        Description = li.Description,
                        Quantity = li.Quantity, 
                        UnitAmount = li.Rate, 
                        AccountCode = li.AccountCode, 
                        TaxType = "OUTPUT",
                        ItemCode = li.ProductId
                    }).ToList(),                
                };

                
                var apiResponse = await _xeroApiService.XeroPostRequest("/Invoices", invoicePayload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var final = JsonConvert.DeserializeObject<XeroInvoiceResponse>(apiResponse.Data.ToString());
                    var invoice = final?.Invoices?.FirstOrDefault();
                    var unifiedInvoice = InvoiceMapper.MapXeroInvoiceToUnified(invoice);
                    return new CommonResponse<object>(200, "Invoice created successfully in Xero", new List<UnifiedInvoice> { unifiedInvoice });
                }
                return new CommonResponse<object>(apiResponse.Status, "Failed to create invoice in Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding invoice to Xero", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> EditInvoice(ConnectionModal connection, string invoiceId, UnifiedInvoiceInputModel input)
        {
            try
            {
                var invoicePayload = new
                {
                    Invoices = new[]
                    {
                        new
                        {
                            InvoiceID = invoiceId, 
                            Type = "ACCREC",
                            Contact = new { Id = input.CustomerId, Name = input.CustomerName },
                            Date = input.InvoiceDate.ToString("yyyy-MM-dd"),
                            DueDate = input.DueDate.ToString("yyyy-MM-dd"),
                            LineItems = input.LineItems.Select(li => new
                            {
                                Description = li.Description,
                                Quantity = li.Quantity,
                                UnitAmount = li.Rate,
                                AccountCode = li.AccountCode,
                                TaxType = "OUTPUT",
                                ItemCode = li.ProductId
                            }).ToList()
                        }
                    }
                };

                var apiResponse = await _xeroApiService.XeroPostRequest($"/Invoices/{invoiceId}", invoicePayload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var final = JsonConvert.DeserializeObject<XeroInvoiceResponse>(apiResponse.Data.ToString());
                    var invoice = final?.Invoices?.FirstOrDefault();
                    var unifiedInvoice = InvoiceMapper.MapXeroInvoiceToUnified(invoice);
                    return new CommonResponse<object>(200, "Invoice updated successfully in Xero", new List<UnifiedInvoice> { unifiedInvoice });
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update invoice in Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating invoice in Xero", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> DeleteInvoice(ConnectionModal connection, string invoiceNumber, string status)
        {
            try
            {
                // Fetch existing invoice
                var fetchResponse = await _xeroApiService.XeroGetRequest($"/Invoices/{invoiceNumber}", connection);
                if (fetchResponse.Status != 200)
                    return new CommonResponse<object>(fetchResponse.Status, "Failed to fetch invoice details", fetchResponse.Data);

                var existingInvoice = JsonConvert.DeserializeObject<XeroInvoiceResponse>(fetchResponse.Data.ToString())
                                        ?.Invoices?.FirstOrDefault();

                if (existingInvoice == null)
                    return new CommonResponse<object>(404, "Invoice not found");

                if (existingInvoice.Status == "VOIDED" || existingInvoice.Status == "PAID")
                    return new CommonResponse<object>(400, $"Cannot modify invoice with status '{existingInvoice.Status}'");

                // Build payload with required fields
                var payload = new
                {
                    InvoiceID = existingInvoice.InvoiceID,
                    Status = status, // DELETED or VOIDED
                    Type= "ACCREC"
                };

                // Proceed to post update

                var apiResponse = await _xeroApiService.XeroPostRequest($"/Invoices/{invoiceNumber}", payload, connection);
                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var final = JsonConvert.DeserializeObject<XeroInvoiceResponse>(apiResponse.Data.ToString());
                    var invoice = final?.Invoices?.FirstOrDefault();
                    var mapped = InvoiceMapper.MapXeroInvoiceToUnified(invoice);

                    var message = status == "DELETED" ? "Invoice deleted successfully from Xero" : "Invoice voided successfully in Xero";
                    return new CommonResponse<object>(200, message, mapped);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to delete or void invoice in Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while deleting/voiding invoice in Xero", ex.Message);
            }
        }



    }
}
