using CsvHelper;
using DataAccess.Helper;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using QuickBookService.Interfaces;
using SharedModels.Models;

using task_14.Data;
using task_14.Models;
using task_14.Services;
using XeroService.Interfaces;

namespace task_14.Repository
{
        public class InvoiceRepository : IInvoiceRepository
        {

        private readonly ApplicationDbContext _context;
        private readonly SyncingFunction _syncingFunction;

        private readonly IQuickBooksInvoiceServices _quickBooksInvoiceServices;
        private readonly IXeroInvoiceService _xeroInvoiceService;
        private readonly IConnectionRepository _connectionRepository;

        public InvoiceRepository(ApplicationDbContext context, SyncingFunction syncingFunction, IQuickBooksInvoiceServices quickBooksInvoiceServices, IConnectionRepository connectionRepository, IXeroInvoiceService xeroInvoiceService)
        {
            _context = context;
            _quickBooksInvoiceServices = quickBooksInvoiceServices;
            _connectionRepository = connectionRepository;
            _xeroInvoiceService = xeroInvoiceService;
            _syncingFunction = syncingFunction;
        }


        public async Task<CommonResponse<object>> SyncInvoices(string platform)
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
                        response = await _quickBooksInvoiceServices.FetchInvoicesFromQuickBooks(connectionResult.Data);
                        break;

                    case "xero":
                        response = await _xeroInvoiceService.FetchInvoicesFromXero(connectionResult.Data);
                        break;

                    default:
                        return new CommonResponse<object>(400, $"Unsupported platform: {platform}");
                }

                if (response.Data == null)
                    return new CommonResponse<object>(response.Status, response.Message);

                var unifiedItems = response.Data as List<UnifiedInvoice>;
                await _syncingFunction.UpdateSyncingInfo(connectionResult.Data.ExternalId, "Invoices", DateTime.UtcNow);
                return await _syncingFunction.StoreUnifiedInvoicesAsync(unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Failed to insert items", ex.Message);
            }
        }



        public async Task<CommonResponse<object>> AddInvoicesAsync(string platform, object input)
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
                        response = await _xeroInvoiceService.AddInvoice(connectionResult.Data, xeroModel);
                        break;
                       
                    case "quickbooks":
                        var qbModel = JsonConvert.DeserializeObject<UnifiedInvoiceInputModel>(input.ToString());
                        response = await _quickBooksInvoiceServices.AddInvoice(connectionResult.Data, qbModel);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedInvoice> invoices && invoices.FirstOrDefault() is UnifiedInvoice updatedInvoice)
           
                {
                    await _syncingFunction.StoreUnifiedInvoicesAsync(invoices);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while adding the item", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> EditInvoicesAsync(string platform,string id, object input)
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
                        response = await _xeroInvoiceService.EditInvoice(connectionResult.Data, id, xeroModel);
                        break;

                    case "quickbooks":
                        var qbModel = JsonConvert.DeserializeObject<UnifiedInvoiceInputModel>(input.ToString());
                        response = await _quickBooksInvoiceServices.EditInvoice(connectionResult.Data,id, qbModel);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Data is List<UnifiedInvoice> invoices && invoices.FirstOrDefault() is UnifiedInvoice updatedInvoice)

                {
                    await _syncingFunction.StoreUnifiedInvoicesAsync(invoices);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while adding the item", ex.Message);
            }
        }
        public async Task<CommonResponse<object>> DeleteInvoiceAsync(string platform, string id,string status)
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
                        response = await _xeroInvoiceService.DeleteInvoice(connectionResult.Data, id,status);
                        break;

                    case "quickbooks":
                        response = await _quickBooksInvoiceServices.DeleteInvoice(connectionResult.Data, id);
                        break;

                    default:
                        return new CommonResponse<object>(400, "Unsupported platform. Use 'xero' or 'quickbooks'.");
                }

                if (response.Status == 200 && response.Data is UnifiedInvoice deletedInvoice)
                {
                    await _syncingFunction.MarkInvoiceAsDeletedAsync(id,status);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "An error occurred while deleting the invoice", ex.Message);
            }
        }


        public async Task<CommonResponse<PagedResponse<InvoiceDto>>> GetInvoicesAsync(
            int page,
            int pageSize,
            string search,
            string sortBy,
            string sortDirection,
            bool pagination =true,
            string source = "all",
            bool isBill=false)
        {
            var query = _context.UnifiedInvoices.AsQueryable();
            query = query.Where(i => i.Status != "DELETED");
            if (!isBill)
            {
                query = query.Where(i => !string.IsNullOrWhiteSpace(i.InvoiceNumber));
            }
            

            if (!string.IsNullOrWhiteSpace(source) && source.ToLower() != "all")
            {
                query = query.Where(i => i.SourceSystem.ToLower() == source.ToLower());
            }


            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i =>
                    EF.Functions.Like(i.Id.ToString(), $"%{search}%") ||
                    EF.Functions.Like(i.CustomerName, $"%{search}%") ||
                    EF.Functions.Like(i.Addresses ?? "", $"%{search}%"));
            }

            query = (sortBy?.ToLower(), sortDirection?.ToLower()) switch
            {
                ("invoicedate", "asc") => query.OrderBy(i => i.InvoiceDate),
                ("invoicedate", "desc") => query.OrderByDescending(i => i.InvoiceDate),
                ("duedate", "asc") => query.OrderBy(i => i.DueDate),
                ("duedate", "desc") => query.OrderByDescending(i => i.DueDate),
                ("totalamount", "asc") => query.OrderBy(i => i.TotalAmount),
                ("totalamount", "desc") => query.OrderByDescending(i => i.TotalAmount),
                ("createdat", "asc") => query.OrderBy(i => i.UpdatedAt),
                _ => query.OrderByDescending(i => i.UpdatedAt),
            };

            var totalRecords = await query.CountAsync();
            if (pagination)
            {
                query = query.Skip((page - 1) * pageSize).Take(pageSize);
            }

            var invoices = await query.ToListAsync();

            var invoiceDtos = invoices.Select(i =>
            {
                string billingAddress = string.Empty;

                if (!string.IsNullOrWhiteSpace(i.Addresses))
                {
                    try
                    {
                        var addressList = JsonConvert.DeserializeObject<List<InvoiceAddress>>(i.Addresses ?? "[]");
                        var billing = addressList?.FirstOrDefault(a => a.Type == "BILLING");
                        billingAddress = billing != null
                            ? $"{billing.Line1}, {billing.City}, {billing.State}, {billing.PostalCode}, {billing.Country}"
                            : "";
                    }
                    catch
                    {
                        billingAddress = ""; 
                    }
                }


                var lineItems = new List<InvoiceLineItem>();
                try
                {
                    lineItems = JsonConvert.DeserializeObject<List<InvoiceLineItem>>(i.LineItems ?? "[]") ?? new List<InvoiceLineItem>();
                }
                catch { }

                return new InvoiceDto
                {
                    Id = i.Id,
                    InvoiceId = i.ExternalId,
                    InvoiceNumber = i.InvoiceNumber,
                    Reference = i.Reference,
                    Status = i.Status,
                    CurrencyCode = i.CurrencyCode,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    CustomerId = i.CustomerId,
                    CustomerName = i.CustomerName,
                    BillingAddress = billingAddress,
                    Subtotal = i.Subtotal,
                    TaxAmount = i.TaxAmount,
                    Total = i.TotalAmount ?? 0,
                    AmountDue = i.AmountDue,
                    AmountPaid = i.AmountPaid,
                    LineAmountTypes = i.LineAmountTypes,
                    SourceSystem = i.SourceSystem,
                    SendLater = false,
                    CreatedAt = i.UpdatedAt ?? i.InvoiceDate,
                    UpdatedAt = i.UpdatedAt,
                    LineItems = lineItems
                };
            }).ToList();


            return new CommonResponse<PagedResponse<InvoiceDto>>(
            200,
            "Invoices fetched successfully",
            new PagedResponse<InvoiceDto>(
                invoiceDtos,
                pagination ? page : 1,
                pagination ? pageSize : invoiceDtos.Count,
                totalRecords
            )
);

        }
        //public async Task<ApiResponse<object>> ProcessCsvFileAsync(string token, string realmId, IFormFile file)
        //{
        //    try
        //    {
        //        var qbUrl = _configuration["QuickBookBaseUrl"];
        //        var records = await ParseCsvFile(file);
        //        var groupedInvoices = records.GroupBy(r => r.InvoiceNumber);

        //        int processedInvoices = 0;
        //        int processedLineItems = 0;

        //        foreach (var group in groupedInvoices)
        //        {
        //            var firstItem = group.First();
        //            var customer = await ProcessCustomer(token, realmId, qbUrl, firstItem);
        //            var lineItems = await ProcessLineItems(token, realmId, qbUrl, group);
        //            await ProcessInvoice(token, realmId, qbUrl, group.Key, firstItem, customer, lineItems);

        //            processedInvoices++;
        //            processedLineItems += lineItems.Count;
        //        }

        //        return new ApiResponse<object>
        //        {
        //            Message = "CSV processed successfully.",
        //            Data = new
        //            {
        //                InvoicesProcessed = processedInvoices,
        //                LineItemsProcessed = processedLineItems
        //            }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<object>
        //        {
        //            Error = "ErrorProcessingCSV",
        //            Message = "An error occurred while processing the CSV file.",
        //            Data = new { ErrorMessage = ex.Message, Trace = ex.StackTrace }
        //        };
        //    }
        //}

        //private async Task<List<InvoiceCsvModel>> ParseCsvFile(IFormFile file)
        //{
        //    using var reader = new StreamReader(file.OpenReadStream());
        //    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        //    return csv.GetRecords<InvoiceCsvModel>().ToList();
        //}

        //private async Task<Customer> ProcessCustomer(string token, string realmId, string qbUrl, InvoiceCsvModel firstItem)
        //{
        //    string displayName = firstItem.CustomerName.Replace("'", "\\'");
        //    string customerQuery = $"SELECT * FROM Customer WHERE DisplayName = '{displayName}'";

        //    dynamic existingCustomer = await ExecuteQuickBooksQuery(token, realmId, qbUrl, customerQuery);
        //    Customer localCustomer = null;
        //    string qbCustomerId = null;

        //    if (existingCustomer?.QueryResponse?.Customer != null)
        //    {
        //        var customer = existingCustomer.QueryResponse.Customer[0];
        //        qbCustomerId = customer?.Id?.ToString();
        //        localCustomer = _context.Customers.FirstOrDefault(p => p.QBId == qbCustomerId);

        //        var inputCustomer = new CustomerInputModel
        //        {
        //            DisplayName = firstItem.CustomerName,
        //            GivenName = customer?.GivenName,
        //            FamilyName = customer?.FamilyName,
        //            EmailAddress = firstItem.CustomerEmail,
        //            PhoneNumber = customer?.PrimaryPhone?.FreeFormNumber,
        //            AddressLine1 = customer?.BillAddr?.Line1,
        //            City = customer?.BillAddr?.City,
        //            CountrySubDivisionCode = customer?.BillAddr?.CountrySubDivisionCode,
        //            PostalCode = customer?.BillAddr?.PostalCode
        //        };

        //        //await _customerRepository.UpdateCustomerAsync(token, realmId, localCustomer.Id.ToString(), inputCustomer);
        //    }
        //    else
        //    {
        //        var inputCustomer = new CustomerInputModel
        //        {
        //            DisplayName = firstItem.CustomerName,
        //            EmailAddress = firstItem.CustomerEmail
        //        };

        //        //await _customerRepository.AddCustomerAsync(token, realmId, inputCustomer);
        //        localCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.DisplayName == inputCustomer.DisplayName);
        //        qbCustomerId = localCustomer.QBId;
        //    }

        //    return localCustomer;
        //}

        //private async Task<List<InvoiceLineItemInputModel>> ProcessLineItems(string token, string realmId, string qbUrl, IGrouping<string, InvoiceCsvModel> group)
        //{
        //    var lineItems = new List<InvoiceLineItemInputModel>();

        //    foreach (var item in group)
        //    {
        //        string productName = item.ItemName.Replace("'", "\\'");
        //        string productQuery = $"SELECT * FROM Item WHERE Name = '{productName}'";

        //        dynamic existingProduct = await ExecuteQuickBooksQuery(token, realmId, qbUrl, productQuery);
        //        int productQBId = 0;
        //        Product localProduct = null;

        //        if (existingProduct?.QueryResponse?.Item != null)
        //        {
        //            var product = existingProduct.QueryResponse.Item[0];
        //            productQBId = product?.Id;
        //            localProduct = _context.Products.FirstOrDefault(p => p.QBItemId == productQBId.ToString());

        //            var inputItem = new ProductInputModel
        //            {
        //                Name = item.ItemName,
        //                Type = "Service",
        //                QBItemId = productQBId.ToString(),
        //                IncomeAccountName = product?.IncomeAccountRef?.name,
        //                IncomeAccountValue = product?.IncomeAccountRef?.value,
        //                Description = item.ItemDescription,
        //                UnitPrice = decimal.Parse(item.Rate),
        //                Taxable = product?.Taxable
        //            };

        //            //await _productRepository.UpdateProductAsync(token, realmId, localProduct.Id, inputItem);
        //        }
        //        else
        //        {
        //            var inputItem = new ProductInputModel
        //            {
        //                Name = item.ItemName,
        //                Type = "Service",
        //                IncomeAccountName = "Sales of Product Income",
        //                IncomeAccountValue = "79",
        //                Description = item.ItemDescription,
        //                UnitPrice = decimal.Parse(item.Rate),
        //                Taxable = false
        //            };

        //            //var result = await _productRepository.AddProductAsync(token, realmId, inputItem);
        //            //productQBId = int.Parse(result.Data?.QBItemId);
        //        }

        //        lineItems.Add(new InvoiceLineItemInputModel
        //        {
        //            ProductId = productQBId,
        //            Description = item.ItemDescription,
        //            Quantity = int.Parse(item.Quantity),
        //            Rate = decimal.Parse(item.Rate)
        //        });
        //    }

        //    return lineItems;
        //}

        //private async Task ProcessInvoice(string token, string realmId, string qbUrl, string invoiceNumber,
        //                               InvoiceCsvModel firstItem, Customer customer, List<InvoiceLineItemInputModel> lineItems)
        //{
        //    string invoiceQuery = $"SELECT * FROM Invoice WHERE Id = '{invoiceNumber}'";

        //    dynamic existingInvoice = await ExecuteQuickBooksQuery(token, realmId, qbUrl, invoiceQuery);

        //    if (existingInvoice?.QueryResponse?.Invoice != null)
        //    {
        //        var invoice = existingInvoice.QueryResponse.Invoice[0];
        //        string qbInvoiceId = invoice?.Id;

        //        var inputInvoice = new InvoiceInputModel
        //        {
        //            InvoiceId = int.Parse(qbInvoiceId),
        //            CustomerId = int.Parse(customer.QBId),
        //            CustomerEmail = firstItem.CustomerEmail,
        //            InvoiceDate = DateTime.Parse(firstItem.InvoiceDate),
        //            DueDate = DateTime.Parse(firstItem.DueDate),
        //            Store = "Main Store",
        //            BillingAddress = firstItem.ItemDescription,
        //            SendLater = false,
        //            LineItems = lineItems
        //        };

        //        await UpdateInvoiceAsync(token, int.Parse(qbInvoiceId), inputInvoice, realmId);
        //    }
        //    else
        //    {
        //        var inputInvoice = new InvoiceInputModel
        //        {
        //            CustomerId = int.Parse(customer.QBId),
        //            CustomerEmail = firstItem.CustomerEmail,
        //            InvoiceDate = DateTime.Parse(firstItem.InvoiceDate),
        //            DueDate = DateTime.Parse(firstItem.DueDate),
        //            Store = "Main Store",
        //            BillingAddress = firstItem.ItemDescription,
        //            SendLater = false,
        //            LineItems = lineItems
        //        };

        //        await CreateInvoiceAsync(token, inputInvoice, realmId);
        //    }
        //}

    }
}
