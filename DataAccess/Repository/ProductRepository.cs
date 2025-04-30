using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using task_14.Data;
using task_14.Models;
using task_14.Services;

namespace task_14.Repository
{
    public class ProductRepository: IProductRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductRepository(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<PagedResponse<Product>> GetPagedProductsAsync(
             string search,
             string sortColumn,
             string sortDirection,
             int page,
             int pageSize,
             bool pagination=true,
             bool active=true)
        {
            
            var query = _context.Products.AsQueryable();

            query = query.Where(p => p.Active == active);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            switch (sortColumn?.ToLower())
            {
                case "name":
                    query = sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name);
                    break;

                case "unitprice":
                    query = sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.UnitPrice)
                        : query.OrderBy(p => p.UnitPrice);
                    break;

                case "type":
                    query = sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Type)
                        : query.OrderBy(p => p.Type);
                    break;

                default:
                    query = sortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name);
                    break;
            }
            var totalRecords = await query.CountAsync();

            if (!pagination)
            {
                return new PagedResponse<Product>(await query.ToListAsync(), 1, pageSize, totalRecords);
            }
   
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PagedResponse<Product>(products, page, pageSize, totalRecords);
        }


        public async Task<ApiResponse<Product>> AddProductAsync(string token, string realmId, ProductInputModel inputModel)
        {
            try
            {
                const string DefaultAssetAccountValue = "81";
                const string DefaultAssetAccountName = "Inventory Asset";
                var product = new Product
                {
                    Name = inputModel.Name,
                    Type = inputModel.Type,
                    FullyQualifiedName = inputModel.FullyQualifiedName ?? inputModel.Name,
                    UnitPrice = inputModel.UnitPrice,
                    Taxable = inputModel.Taxable,
                    Description = inputModel.Description ?? "",
                    IncomeAccountValue = inputModel.IncomeAccountValue ?? "" ,
                    IncomeAccountName = inputModel.IncomeAccountName ?? "",
                    ExpenseAccountValue = inputModel.ExpenseAccountValue ?? "",
                    ExpenseAccountName = inputModel.ExpenseAccountName ?? "",
                    CreatedAt = DateTime.UtcNow,
                    Active = true,
                    QBItemId = "",
                    SyncToken = ""
                };

                dynamic quickBooksProduct;

                if (inputModel.Type == "Inventory")
                {
                    quickBooksProduct = new
                    {
                        Name = inputModel.Name,
                        Type = "Inventory",
                        TrackQtyOnHand = true,
                        QtyOnHand = 0,
                        InvStartDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        UnitPrice = inputModel.UnitPrice,
                        Taxable = inputModel.Taxable,
                        IncomeAccountRef = new { value = inputModel.IncomeAccountValue, name = inputModel.IncomeAccountName },
                        ExpenseAccountRef = new { value = inputModel.ExpenseAccountValue, name = inputModel.ExpenseAccountName},
                        AssetAccountRef = new { value = "81", name = "Inventory Asset" },
                        Description = inputModel.Description
                    };
                }
                else
                {
                    quickBooksProduct = new
                    {
                        Name = inputModel.Name,
                        Type = "Service",
                        UnitPrice = inputModel.UnitPrice,
                        Taxable = inputModel.Taxable,
                        IncomeAccountRef = new { value = inputModel.IncomeAccountValue, name = inputModel.IncomeAccountName },
                        AssetAccountRef = new { value = "81", name = "Inventory Asset" },
                        Description = inputModel.Description
                    };
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(JsonConvert.SerializeObject(quickBooksProduct), Encoding.UTF8, "application/json");
                var qburl = _configuration["QuickBookBaseUrl"];
                var url = $"{qburl}{realmId}/item";

                var response = await client.PostAsync(url, content);
                var quickBooksResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic responseData = JsonConvert.DeserializeObject(quickBooksResponse);
                    product.QBItemId = responseData.Item?.Id?.ToString() ?? "";
                    product.SyncToken = responseData.Item?.SyncToken?.ToString() ?? "";

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    return new ApiResponse<Product>(product, "Product added successfully to QuickBooks and local DB.");
                }
                else
                {
                    return new ApiResponse<Product>(product, "Product saved  QuickBooks failed.", quickBooksResponse);
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Product>(null, "An error occurred while adding product.", ex.Message);
            }
        }

        private async Task<dynamic> FetchProductFromQuickBooksAsync(string token, string realmId, string qbItemId)
        {
            var qburl = _configuration["QuickBookBaseUrl"];
            var url = $"{qburl}{realmId}/item/{qbItemId}?minorversion=75";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch product from QuickBooks: {content}");

            dynamic data = JsonConvert.DeserializeObject(content);
            return data.Item;
        }


        public async Task<ApiResponse<Product>> UpdateProductAsync(string token, string realmId, int id, ProductInputModel inputModel)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return new ApiResponse<Product>(null, "Product not found.");

            if (string.IsNullOrEmpty(product.QBItemId))
                return new ApiResponse<Product>(null, "Product has not been synced with QuickBooks yet.");

            try
            {
                dynamic existingQBProduct = await FetchProductFromQuickBooksAsync(token, realmId, product.QBItemId);

                dynamic updateObj;
                if (inputModel.Type == "Inventory")
                {
                    updateObj = new
                    {
                        Id = existingQBProduct.Id,
                        SyncToken = existingQBProduct.SyncToken,
                        Name = inputModel.Name,
                        Type = "Inventory",
                        TrackQtyOnHand = true,
                        UnitPrice = inputModel.UnitPrice,
                        Taxable = inputModel.Taxable,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" },
                        ExpenseAccountRef = new { value = "80", name = "Cost of Goods Sold" },
                        AssetAccountRef = new { value = "81", name = "Inventory Asset" },
                        Description = inputModel.Description
                    };
                }
                else
                {
                    updateObj = new
                    {
                        Id = existingQBProduct.Id,
                        SyncToken = existingQBProduct.SyncToken,
                        Name = inputModel.Name,
                        Type = "Service",
                        UnitPrice = inputModel.UnitPrice,
                        Taxable = inputModel.Taxable,
                        IncomeAccountRef = new { value = inputModel.IncomeAccountValue, name = inputModel.IncomeAccountName },
                        Description = inputModel.Description
                    };
                }

                var json = JsonConvert.SerializeObject(updateObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var qburl = _configuration["QuickBookBaseUrl"];
                var url = $"{qburl}{realmId}/item?operation=update&minorversion=75";
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ApiResponse<Product>(null, "Failed to update product in QuickBooks.", responseContent);

                var updatedQBProduct = JsonConvert.DeserializeObject<dynamic>(responseContent);
                string syncToken = updatedQBProduct.Item.SyncToken.ToString();

              
                product.Name = inputModel.Name;
                product.Type = inputModel.Type;
                product.UnitPrice = inputModel.UnitPrice;
                product.Taxable = inputModel.Taxable;
                product.Description = inputModel.Description ?? "";
                product.SyncToken = syncToken;
                product.IncomeAccountValue = inputModel.IncomeAccountValue ?? "";
                product.IncomeAccountName = inputModel.IncomeAccountName ?? "";
                product.ExpenseAccountValue = inputModel.ExpenseAccountValue ?? "";
                product.ExpenseAccountName = inputModel.ExpenseAccountName ?? "";

                await _context.SaveChangesAsync();

                return new ApiResponse<Product>(product, "Product updated in QuickBooks and local DB.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Product>(null, "Exception occurred while updating product.", ex.Message);
            }
        }


        public async Task<ApiResponse<string>> DeleteProductAsync(string token, string realmId, int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return new ApiResponse<string>(null, "Product not found", "Invalid ID");

            try
            {
                dynamic qbProduct = await FetchProductFromQuickBooksAsync(token, realmId, product.QBItemId);

                var deleteObj = new
                {
                    Id = qbProduct.Id,
                    SyncToken = qbProduct.SyncToken,
                    Name = qbProduct.Name,
                    Type = qbProduct.Type,
                    Active = false,
                    IncomeAccountRef = qbProduct.IncomeAccountRef,
                    ExpenseAccountRef = qbProduct.Type == "Inventory" ? qbProduct.ExpenseAccountRef : null,
                    AssetAccountRef = qbProduct.Type == "Inventory" ? qbProduct.AssetAccountRef : null
                };

                var json = JsonConvert.SerializeObject(deleteObj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var qburl = _configuration["QuickBookBaseUrl"];
                var url = $"{qburl}{realmId}/item?operation=update&minorversion=75";

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync(url, content);
                var qbResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ApiResponse<string>(null, "QuickBooks deletion failed", qbResponse);

                product.Active = false;
                product.SyncToken = qbProduct.SyncToken;
                await _context.SaveChangesAsync();

                return new ApiResponse<string>("Deleted", "Product successfully marked inactive in both QuickBooks and local DB.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(null, "Exception occurred during QuickBooks product deletion.", ex.Message);
            }
        }



        public async Task<int> GetTotalProductCountAsync(string? search)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }
            return await query.CountAsync();
        }

        public async Task<bool> SyncItemsFromQuickBooksAsync(string authorization, string realmId)
        {
            try
            {
                var qburl = _configuration["QuickBookBaseUrl"];
                var token = authorization?.Replace("Bearer ", "");
                string url = $"{qburl}{realmId}/query?query=SELECT * FROM Item where Active IN (true,false)";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("QueryResponse", out var queryResponse) &&
                    queryResponse.TryGetProperty("Item", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var type = item.GetProperty("Type").GetString();
                        var entity = new Product
                        {
                            Name = item.TryGetProperty("Name", out var name) ? name.GetString() : "",
                            Type = type,
                            FullyQualifiedName = item.TryGetProperty("FullyQualifiedName", out var fullName) ? fullName.GetString() : "",
                            UnitPrice = item.TryGetProperty("UnitPrice", out var unitPrice) ? unitPrice.GetDecimal() : 0,
                            Taxable = item.TryGetProperty("Taxable", out var taxable) && taxable.GetBoolean(),
                            QBItemId = item.GetProperty("Id").GetString(),
                            SyncToken = item.GetProperty("SyncToken").GetString(),
                            Active = item.TryGetProperty("Active", out var active) && active.GetBoolean()
                        };

                        entity.Description = item.TryGetProperty("Description", out var desc) ? desc.GetString() : "";

                        if (item.TryGetProperty("IncomeAccountRef", out var incomeRef))
                        {
                            entity.IncomeAccountValue = incomeRef.TryGetProperty("value", out var incomeValue) ? incomeValue.GetString() : "";
                            entity.IncomeAccountName = incomeRef.TryGetProperty("name", out var incomeName) ? incomeName.GetString() : "";
                        }
                        else
                        {
                            entity.IncomeAccountValue = "";
                            entity.IncomeAccountName = "";
                        }
                        if (type == "NonInventory" && item.TryGetProperty("ExpenseAccountRef", out var expenseRef))
                        {
                            entity.ExpenseAccountValue = expenseRef.TryGetProperty("value", out var expVal) ? expVal.GetString() : "";
                            entity.ExpenseAccountName = expenseRef.TryGetProperty("name", out var expName) ? expName.GetString() : "";
                        }
                        else
                        {
                            entity.ExpenseAccountValue = "";
                            entity.ExpenseAccountName = "";
                        }

                        if (item.TryGetProperty("MetaData", out var meta) &&
                            meta.TryGetProperty("CreateTime", out var createTime))
                        {
                            entity.CreatedAt = DateTime.TryParse(createTime.GetString(), out var parsedDate) ? parsedDate : DateTime.MinValue;
                        }
                        else
                        {
                            entity.CreatedAt = DateTime.MinValue;
                        }

                        var exists = await _context.Products
                            .AnyAsync(x => x.QBItemId == entity.QBItemId);

                        if (!exists)
                            _context.Products.Add(entity);
                    }
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ApiResponse<string>> MarkProductActiveAsync(string token, string realmId, int productId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var qbUrl = _configuration["QuickBookBaseUrl"];
                var getUrl = $"{qbUrl}{realmId}/item/{productId}?minorversion=65";
                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var getResponse = await client.SendAsync(getRequest);
                var getJson = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<string>(error: getJson, message: "Failed to fetch product before activation.");
                }

                var existingProduct = JsonConvert.DeserializeObject<QuickBooksItemResponse>(getJson);
                if (existingProduct?.Item == null)
                {
                    return new ApiResponse<string>(error: "Invalid product response", message: "Product data not found in response.");
                }

                var payload = new
                {
                    Id = existingProduct.Item.Id,
                    Name = existingProduct.Item.Name,
                    SyncToken = existingProduct.Item.SyncToken,
                    Active = true
                };

                var json = JsonConvert.SerializeObject(payload);
                var updateUrl = $"{qbUrl}{realmId}/item?operation=update";

                var updateRequest = new HttpRequestMessage(HttpMethod.Post, updateUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                updateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

              
                var updateResponse = await client.SendAsync(updateRequest);
                var updateJson = await updateResponse.Content.ReadAsStringAsync();

                if (!updateResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<string>(error: updateJson, message: "Failed to activate product in QuickBooks.");
                }

                var updatedProduct = JsonConvert.DeserializeObject<QuickBooksItemResponse>(updateJson);

                var localProduct = await _context.Products.FirstOrDefaultAsync(c => c.QBItemId == productId.ToString());
                if (localProduct != null && updatedProduct?.Item != null)
                {
                    localProduct.SyncToken = updatedProduct.Item.SyncToken;
                    localProduct.Active = true;
                    await _context.SaveChangesAsync();
                }

                return new ApiResponse<string>("Product activated successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>(error: ex.Message, message: "Error occurred while activating Product.");
            }
        }

        public class QuickBooksItemResponse
        {
            public Item Item { get; set; }
        }

        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string SyncToken { get; set; }
            public bool Active { get; set; }
        }


    }
}
