using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using task_14.Data;
using task_14.Models;
using task_14.Services;

namespace task_14.Repository
{
        public class InvoiceRepository : IInvoiceRepository
        {

            private readonly ApplicationDbContext _context;
            private readonly IConfiguration _configuration;
            private readonly ICustomerRepository _customerRepository;
            private readonly IProductRepository _productRepository;
            

        public InvoiceRepository(
            ApplicationDbContext context,
            IConfiguration configuration,
            ICustomerRepository customerRepository,
            IProductRepository productRepository
           )
        {
            _context = context;
            _configuration = configuration;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            
        }

        public async Task<ApiResponse<object>> SyncInvoicesFromQuickBooksAsync(string token, string realmId)
        {
            try
            {
                var qburl = _configuration["QuickBookBaseUrl"];
                string url = $"{qburl}{realmId}/query?query=select * from Invoice&minorversion=75";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                dynamic data = JsonConvert.DeserializeObject(json);
                var invoices = data.QueryResponse?.Invoice;

                if (invoices == null)
                    return new ApiResponse<object>
                    {
                        Error = "NoInvoicesFound",
                        Message = "No invoices found from QuickBooks",
                        Data= new
                        {
                            InvoicesSynced = 0,
                            LineItemsSynced = 0
                        }
                    };

                var newInvoices = new List<Invoice>();
                var newLineItems = new List<InvoiceLineItem>();

                foreach (var item in invoices)
                {
                    string qbId = item.Id?.ToString();
                    if (qbId == null) continue;

                    bool exists = await _context.Invoices.AnyAsync(x => x.InvoiceId == int.Parse(qbId));

                    if (!exists)
                    {
                        var invoice = new Invoice
                        {
                            InvoiceId = int.Parse(qbId),
                            CustomerId = int.TryParse((string?)item.CustomerRef?.value, out var cid) ? cid : 0,
                            InvoiceDate = DateTime.TryParse((string?)item.MetaData?.CreateTime, out var invDate) ? invDate : DateTime.MinValue,
                            DueDate = DateTime.TryParse((string?)item.DueDate, out var due) ? due : DateTime.MinValue,
                            Store = "Main Store",
                            BillingAddress = string.Join(", ",
                                new[] {
                            item.BillAddr?.Line1,
                            item.BillAddr?.Line2,
                            item.BillAddr?.Line3,
                            item.BillAddr?.Line4
                                }.Where(x => !string.IsNullOrWhiteSpace((string?)x))),
                            Subtotal = item.Line != null && item.Line.Count > 0 ? (decimal?)item.Line[item.Line.Count - 1]?.Amount ?? 0 : 0,
                            Total = (decimal?)item.TotalAmt ?? 0,
                            SendLater = false,
                            CreatedAt = DateTime.TryParse((string?)item.MetaData?.CreateTime, out var cAt) ? cAt : DateTime.Now,
                            UpdatedAt = DateTime.TryParse((string?)item.MetaData?.LastUpdatedTime, out var uAt) ? uAt : DateTime.Now
                        };

                        newInvoices.Add(invoice);
                    }

                    var lineItems = item.Line;
                    if (lineItems != null)
                    {
                        foreach (var lineItem in lineItems)
                        {
                            string lineId = lineItem.Id;
                            if (lineId == null) continue;

                            var existsLine = await _context.InvoiceLineItem.AnyAsync(x => x.LineId == lineId);

                            if (!existsLine)
                            {
                                var lineItemObj = new InvoiceLineItem
                                {
                                    LineId = lineId,
                                    InvoiceId = int.Parse(qbId),
                                    ProductId = (string?)lineItem.SalesItemLineDetail?.ItemRef?.value ?? "Unknown",
                                    Description = (string?)lineItem.Description ?? "",
                                    Quantity = (int?)lineItem.SalesItemLineDetail?.Qty ?? 0,
                                    Rate = (decimal?)lineItem.SalesItemLineDetail?.UnitPrice ?? 0,
                                    Amount = (decimal?)lineItem.Amount ?? 0
                                };

                                newLineItems.Add(lineItemObj);
                            }
                        }
                    }
                }

                if (newInvoices.Any())
                {
                    await _context.Invoices.AddRangeAsync(newInvoices);
                    await _context.SaveChangesAsync();
                }

                if (newLineItems.Any())
                {
                    await _context.InvoiceLineItem.AddRangeAsync(newLineItems);
                    await _context.SaveChangesAsync();
                }

                return new ApiResponse<object>
                {
                    Message = "Invoices synced successfully from QuickBooks.",
                    Data = new
                    {
                        InvoicesSynced = newInvoices.Count,
                        LineItemsSynced = newLineItems.Count
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Error = "ErrorSyncingInvoices",
                    Message = "Error syncing invoices",
                    Data = new { ErrorMessage = ex.Message, Trace = ex.StackTrace }
                };
            }
        }

        public async Task<PagedResponse<InvoiceDto>> GetInvoicesAsync(
                     int page,
                     int pageSize,
                     string search,
                     string sortBy,
                     string sortDirection,
                     bool pagination)
            {
                var query = _context.Invoices.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(i =>
                        EF.Functions.Like(i.InvoiceId.ToString(), $"%{search}%") ||
                        EF.Functions.Like(i.Store, $"%{search}%") ||
                        EF.Functions.Like(i.BillingAddress, $"%{search}%"));
                }

                query = (sortBy?.ToLower(), sortDirection?.ToLower()) switch
                {
                    ("invoicedate", "asc") => query.OrderBy(i => i.InvoiceDate),
                    ("invoicedate", "desc") => query.OrderByDescending(i => i.InvoiceDate),
                    ("duedate", "asc") => query.OrderBy(i => i.DueDate),
                    ("duedate", "desc") => query.OrderByDescending(i => i.DueDate),
                    ("total", "asc") => query.OrderBy(i => i.Total),
                    ("total", "desc") => query.OrderByDescending(i => i.Total),
                    ("createdat", "asc") => query.OrderBy(i => i.CreatedAt),
                    _ => query.OrderByDescending(i => i.CreatedAt),
                };

                var totalRecords = await query.CountAsync();
                if (pagination)
                {
                    query = query.Skip((page - 1) * pageSize).Take(pageSize);
                }

                var invoices = await query.ToListAsync();
                var invoiceIds = invoices.Select(i => i.InvoiceId).ToList();

                var lineItems = await _context.InvoiceLineItem
                                        .Where(li => invoiceIds.Contains(li.InvoiceId))
                                        .ToListAsync();

                var invoiceDtos = invoices.Select(i => new InvoiceDto
                {
                    Id = i.Id,
                    InvoiceId = i.InvoiceId,
                    CustomerId = i.CustomerId,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    Store = i.Store,
                    BillingAddress = i.BillingAddress,
                    Subtotal = i.Subtotal,
                    Total = i.Total,
                    SendLater = i.SendLater,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    LineItems = lineItems.Where(li => li.InvoiceId == i.InvoiceId).ToList()
                }).ToList();

                return new PagedResponse<InvoiceDto>(
                    invoiceDtos,
                    pagination ? page : 1,
                    pagination ? pageSize : invoiceDtos.Count,
                    totalRecords
                );
            }

        public async Task<ApiResponse<object>> CreateInvoiceAsync(string token, InvoiceInputModel model, string realmId)
        {
            try
            {
                var qbInvoicePayload = new
                {
                    CustomerRef = new { value = model.CustomerId.ToString() },
                    TxnDate = model.InvoiceDate.ToString("yyyy-MM-dd"),
                    DueDate = model.DueDate.ToString("yyyy-MM-dd"),
                    BillAddr = new { Line1 = model.BillingAddress },
                    PrivateNote = model.Store,
                    Line = model.LineItems.Select(li => new
                    {
                        DetailType = "SalesItemLineDetail",
                        Amount = li.Quantity * li.Rate,
                        Description = li.Description,
                        SalesItemLineDetail = new
                        {
                            ItemRef = new { value = li.ProductId.ToString() },
                            Qty = li.Quantity,
                            UnitPrice = li.Rate
                        }
                    }).ToList()
                };

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var qburl = _configuration["QuickBookBaseUrl"];
                var content = new StringContent(JsonConvert.SerializeObject(qbInvoicePayload), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{qburl}{realmId}/invoice", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<object>(null, null, $"QuickBooks error: {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseBody);
                var qbInvoiceId = jsonResponse["Invoice"]?["Id"]?.ToString();
                var qbTotalAmount = jsonResponse["Invoice"]?["TotalAmt"]?.ToObject<decimal>();

                var invoice = new Invoice
                {
                    InvoiceId = int.Parse(qbInvoiceId),
                    CustomerId = model.CustomerId,
                    InvoiceDate = model.InvoiceDate,
                    DueDate = model.DueDate,
                    Store = model.Store,
                    BillingAddress = model.BillingAddress,
                    Subtotal = model.LineItems.Sum(li => li.Quantity * li.Rate),
                    Total = qbTotalAmount ?? 0,
                    SendLater = model.SendLater,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                foreach (var line in model.LineItems)
                {
                    var localLineItem = new InvoiceLineItem
                    {
                        LineId = Guid.NewGuid().ToString(),
                        InvoiceId = invoice.InvoiceId,
                        ProductId = line.ProductId.ToString(),
                        Description = line.Description,
                        Quantity = (int)line.Quantity,
                        Rate = line.Rate,
                        Amount = line.Quantity * line.Rate
                    };
                    _context.InvoiceLineItem.Add(localLineItem);
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<object>(new { invoiceId = invoice.InvoiceId }, "Invoice created successfully");
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(null, "Failed to create invoice", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> UpdateInvoiceAsync(string token, int invoiceId, InvoiceInputModel model, string realmId)
        {
            try
            {
                var existingInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
                if (existingInvoice == null)
                {
                    return new ApiResponse<object>(null, "Invoice not found in local DB.", "NotFound");
                }

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var qburl = _configuration["QuickBookBaseUrl"];
                var fetchResponse = await httpClient.GetAsync($"{qburl}{realmId}/invoice/{invoiceId}");
                if (!fetchResponse.IsSuccessStatusCode)
                {
                    var fetchError = await fetchResponse.Content.ReadAsStringAsync();
                    return new ApiResponse<object>(null, "Failed to fetch invoice from QuickBooks.", fetchError);
                }

                var fetchBody = await fetchResponse.Content.ReadAsStringAsync();
                var jsonInvoice = JObject.Parse(fetchBody)["Invoice"];
                var syncToken = jsonInvoice?["SyncToken"]?.ToString();
       
                var qbUpdatePayload = new
                {
                    Id = invoiceId,
                    SyncToken = syncToken,
                    CustomerRef = new { value = model.CustomerId.ToString() },
                    TxnDate = model.InvoiceDate.ToString("yyyy-MM-dd"),
                    DueDate = model.DueDate.ToString("yyyy-MM-dd"),
                    BillAddr = new { Line1 = model.BillingAddress },
                    PrivateNote = model.Store,
                    Line = model.LineItems.Select(li => new
                    {
                        DetailType = "SalesItemLineDetail",
                        Amount = li.Quantity * li.Rate,
                        Description = li.Description,
                        SalesItemLineDetail = new
                        {
                            ItemRef = new { value = li.ProductId.ToString() },
                            Qty = li.Quantity,
                            UnitPrice = li.Rate
                        }
                    }).ToList()
                };

                var content = new StringContent(JsonConvert.SerializeObject(qbUpdatePayload), Encoding.UTF8, "application/json");

                var updateResponse = await httpClient.PostAsync($"{qburl}{realmId}/invoice?operation=update", content);
                if (!updateResponse.IsSuccessStatusCode)
                {
                    var updateError = await updateResponse.Content.ReadAsStringAsync();
                    return new ApiResponse<object>(null, "QuickBooks update failed.", updateError);
                }
                existingInvoice.CustomerId = model.CustomerId;
                existingInvoice.InvoiceDate = model.InvoiceDate;
                existingInvoice.DueDate = model.DueDate;
                existingInvoice.Store = model.Store;
                existingInvoice.BillingAddress = model.BillingAddress;
                existingInvoice.Subtotal = model.LineItems.Sum(li => li.Quantity * li.Rate);
                existingInvoice.Total = model.LineItems.Sum(li => li.Quantity * li.Rate);
                existingInvoice.SendLater = model.SendLater;
                existingInvoice.UpdatedAt = DateTime.UtcNow;
                var oldLineItems = _context.InvoiceLineItem.Where(li => li.InvoiceId == existingInvoice.Id);
                _context.InvoiceLineItem.RemoveRange(oldLineItems);

                foreach (var line in model.LineItems)
                {
                    _context.InvoiceLineItem.Add(new InvoiceLineItem
                    {
                        LineId = Guid.NewGuid().ToString(),
                        InvoiceId = existingInvoice.Id,
                        ProductId = line.ProductId.ToString(),
                        Description = line.Description,
                        Quantity = (int)line.Quantity,
                        Rate = line.Rate,
                        Amount = line.Quantity * line.Rate
                    });
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<object>(
                    data: new { invoiceId = existingInvoice.InvoiceId },
                    message: "Invoice updated successfully"
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(null, "Unexpected error occurred", $"{ex.Message}\n{ex.StackTrace}");
            }
        }


        public async Task<ApiResponse<object>> DeleteInvoiceAsync(string token, int invoiceId, string realmId)
        {
            try
            {
                var existingInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
                if (existingInvoice == null)
                {
                    return new ApiResponse<object>(null, "Invoice not found in local DB.", "NotFound");
                }

                var httpClient = new HttpClient();
                var qburl = _configuration["QuickBookBaseUrl"];
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var fetchResponse = await httpClient.GetAsync($"{qburl}{realmId}/invoice/{invoiceId}");
                if (!fetchResponse.IsSuccessStatusCode)
                {
                    var fetchError = await fetchResponse.Content.ReadAsStringAsync();
                    return new ApiResponse<object>(null, "Failed to fetch invoice from QuickBooks.", fetchError);
                }

                var fetchBody = await fetchResponse.Content.ReadAsStringAsync();
                var jsonInvoice = JObject.Parse(fetchBody)["Invoice"];
                var syncToken = jsonInvoice?["SyncToken"]?.ToString();
                var qbDeletePayload = new
                {
                    Id = invoiceId.ToString(),
                    SyncToken = syncToken
                };

                var deleteContent = new StringContent(JsonConvert.SerializeObject(qbDeletePayload), Encoding.UTF8, "application/json");

                var deleteResponse = await httpClient.PostAsync($"{qburl}{realmId}/invoice?operation=delete", deleteContent);
                if (!deleteResponse.IsSuccessStatusCode)
                {
                    var deleteError = await deleteResponse.Content.ReadAsStringAsync();
                    return new ApiResponse<object>(null, "QuickBooks delete failed.", deleteError);
                }

                var lineItems = _context.InvoiceLineItem.Where(li => li.InvoiceId == existingInvoice.Id);
                _context.InvoiceLineItem.RemoveRange(lineItems);
                _context.Invoices.Remove(existingInvoice);
                await _context.SaveChangesAsync();

                return new ApiResponse<object>(new { invoiceId }, "Invoice deleted successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>(null, "Unexpected error occurred during delete.", $"{ex.Message}\n{ex.StackTrace}");
            }
        }

        public async Task<ApiResponse<object>> ProcessCsvFileAsync(string token, string realmId, IFormFile file)
        {
            try
            {
                var qbUrl = _configuration["QuickBookBaseUrl"];
                var records = await ParseCsvFile(file);
                var groupedInvoices = records.GroupBy(r => r.InvoiceNumber);

                int processedInvoices = 0;
                int processedLineItems = 0;

                foreach (var group in groupedInvoices)
                {
                    var firstItem = group.First();
                    var customer = await ProcessCustomer(token, realmId, qbUrl, firstItem);
                    var lineItems = await ProcessLineItems(token, realmId, qbUrl, group);
                    await ProcessInvoice(token, realmId, qbUrl, group.Key, firstItem, customer, lineItems);

                    processedInvoices++;
                    processedLineItems += lineItems.Count;
                }

                return new ApiResponse<object>
                {
                    Message = "CSV processed successfully.",
                    Data = new
                    {
                        InvoicesProcessed = processedInvoices,
                        LineItemsProcessed = processedLineItems
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Error = "ErrorProcessingCSV",
                    Message = "An error occurred while processing the CSV file.",
                    Data = new { ErrorMessage = ex.Message, Trace = ex.StackTrace }
                };
            }
        }

        private async Task<List<InvoiceCsvModel>> ParseCsvFile(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<InvoiceCsvModel>().ToList();
        }

        private async Task<Customer> ProcessCustomer(string token, string realmId, string qbUrl, InvoiceCsvModel firstItem)
        {
            string displayName = firstItem.CustomerName.Replace("'", "\\'");
            string customerQuery = $"SELECT * FROM Customer WHERE DisplayName = '{displayName}'";

            dynamic existingCustomer = await ExecuteQuickBooksQuery(token, realmId, qbUrl, customerQuery);
            Customer localCustomer = null;
            string qbCustomerId = null;

            if (existingCustomer?.QueryResponse?.Customer != null)
            {
                var customer = existingCustomer.QueryResponse.Customer[0];
                qbCustomerId = customer?.Id?.ToString();
                localCustomer = _context.Customers.FirstOrDefault(p => p.QBId == qbCustomerId);

                var inputCustomer = new CustomerInputModel
                {
                    DisplayName = firstItem.CustomerName,
                    GivenName = customer?.GivenName,
                    FamilyName = customer?.FamilyName,
                    EmailAddress = firstItem.CustomerEmail,
                    PhoneNumber = customer?.PrimaryPhone?.FreeFormNumber,
                    AddressLine1 = customer?.BillAddr?.Line1,
                    City = customer?.BillAddr?.City,
                    CountrySubDivisionCode = customer?.BillAddr?.CountrySubDivisionCode,
                    PostalCode = customer?.BillAddr?.PostalCode
                };

                //await _customerRepository.UpdateCustomerAsync(token, realmId, localCustomer.Id.ToString(), inputCustomer);
            }
            else
            {
                var inputCustomer = new CustomerInputModel
                {
                    DisplayName = firstItem.CustomerName,
                    EmailAddress = firstItem.CustomerEmail
                };

                //await _customerRepository.AddCustomerAsync(token, realmId, inputCustomer);
                localCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.DisplayName == inputCustomer.DisplayName);
                qbCustomerId = localCustomer.QBId;
            }

            return localCustomer;
        }

        private async Task<List<InvoiceLineItemInputModel>> ProcessLineItems(string token, string realmId, string qbUrl, IGrouping<string, InvoiceCsvModel> group)
        {
            var lineItems = new List<InvoiceLineItemInputModel>();

            foreach (var item in group)
            {
                string productName = item.ItemName.Replace("'", "\\'");
                string productQuery = $"SELECT * FROM Item WHERE Name = '{productName}'";

                dynamic existingProduct = await ExecuteQuickBooksQuery(token, realmId, qbUrl, productQuery);
                int productQBId = 0;
                Product localProduct = null;

                if (existingProduct?.QueryResponse?.Item != null)
                {
                    var product = existingProduct.QueryResponse.Item[0];
                    productQBId = product?.Id;
                    localProduct = _context.Products.FirstOrDefault(p => p.QBItemId == productQBId.ToString());

                    var inputItem = new ProductInputModel
                    {
                        Name = item.ItemName,
                        Type = "Service",
                        QBItemId = productQBId.ToString(),
                        IncomeAccountName = product?.IncomeAccountRef?.name,
                        IncomeAccountValue = product?.IncomeAccountRef?.value,
                        Description = item.ItemDescription,
                        UnitPrice = decimal.Parse(item.Rate),
                        Taxable = product?.Taxable
                    };

                    await _productRepository.UpdateProductAsync(token, realmId, localProduct.Id, inputItem);
                }
                else
                {
                    var inputItem = new ProductInputModel
                    {
                        Name = item.ItemName,
                        Type = "Service",
                        IncomeAccountName = "Sales of Product Income",
                        IncomeAccountValue = "79",
                        Description = item.ItemDescription,
                        UnitPrice = decimal.Parse(item.Rate),
                        Taxable = false
                    };

                    var result = await _productRepository.AddProductAsync(token, realmId, inputItem);
                    productQBId = int.Parse(result.Data?.QBItemId);
                }

                lineItems.Add(new InvoiceLineItemInputModel
                {
                    ProductId = productQBId,
                    Description = item.ItemDescription,
                    Quantity = int.Parse(item.Quantity),
                    Rate = decimal.Parse(item.Rate)
                });
            }

            return lineItems;
        }

        private async Task ProcessInvoice(string token, string realmId, string qbUrl, string invoiceNumber,
                                       InvoiceCsvModel firstItem, Customer customer, List<InvoiceLineItemInputModel> lineItems)
        {
            string invoiceQuery = $"SELECT * FROM Invoice WHERE Id = '{invoiceNumber}'";

            dynamic existingInvoice = await ExecuteQuickBooksQuery(token, realmId, qbUrl, invoiceQuery);

            if (existingInvoice?.QueryResponse?.Invoice != null)
            {
                var invoice = existingInvoice.QueryResponse.Invoice[0];
                string qbInvoiceId = invoice?.Id;

                var inputInvoice = new InvoiceInputModel
                {
                    InvoiceId = int.Parse(qbInvoiceId),
                    CustomerId = int.Parse(customer.QBId),
                    CustomerEmail = firstItem.CustomerEmail,
                    InvoiceDate = DateTime.Parse(firstItem.InvoiceDate),
                    DueDate = DateTime.Parse(firstItem.DueDate),
                    Store = "Main Store",
                    BillingAddress = firstItem.ItemDescription,
                    SendLater = false,
                    LineItems = lineItems
                };

                await UpdateInvoiceAsync(token, int.Parse(qbInvoiceId), inputInvoice, realmId);
            }
            else
            {
                var inputInvoice = new InvoiceInputModel
                {
                    CustomerId = int.Parse(customer.QBId),
                    CustomerEmail = firstItem.CustomerEmail,
                    InvoiceDate = DateTime.Parse(firstItem.InvoiceDate),
                    DueDate = DateTime.Parse(firstItem.DueDate),
                    Store = "Main Store",
                    BillingAddress = firstItem.ItemDescription,
                    SendLater = false,
                    LineItems = lineItems
                };

                await CreateInvoiceAsync(token, inputInvoice, realmId);
            }
        }

        private async Task<dynamic> ExecuteQuickBooksQuery(string token, string realmId, string qbUrl, string query)
        {
            string url = $"{qbUrl}{realmId}/query?query={Uri.EscapeDataString(query)}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(json);
        }
    }
}
