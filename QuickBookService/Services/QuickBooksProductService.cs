
using Newtonsoft.Json;
using QuickBookService.Interfaces;
using QuickBookService.Mappers;
using SharedModels.QuickBooks.Models;
using SharedModels.Models;
using System.Threading.Tasks;
using SharedModels.Xero.Models;

namespace QuickBookService.Services
{
    public class QuickBooksProductService : IQuickBooksProductService
    {
        private readonly IQuickBooksApiService _quickBooksApiService;
        public QuickBooksProductService(IQuickBooksApiService quickBooksApiService)
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

        public async Task<CommonResponse<object>> FetchProductsFromQuickBooks(ConnectionModal connection)
        {
            try
            {
                int startPosition = 1;
                const int maxResults = 1000;
                var lastSync = GetLastSyncedDate(connection.SyncingInfo, "Products");

                var allProducts = new List<QuickBooksProduct>();

                while (true)
                {
                    string query = $"SELECT * FROM Item WHERE Metadata.LastUpdatedTime > '{lastSync:yyyy-MM-ddTHH:mm:ssZ}' " +
                                   $"STARTPOSITION {startPosition} MAXRESULTS {maxResults}";
                    var apiResponse = await _quickBooksApiService.QuickBooksGetRequest(
                        $"query?query={Uri.EscapeDataString(query)}", connection);
                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch products", apiResponse.Data);
                    var result = JsonConvert.DeserializeObject<ProductRoot>(apiResponse.Data.ToString());
                    var products = result?.QueryResponse?.Item;
                    if (products == null || products.Count == 0)
                        break;
                    allProducts.AddRange(products);
                    if (products.Count < maxResults)
                        break;
                    startPosition += maxResults;
                }

                if (allProducts.Count == 0)
                    return new CommonResponse<object>(200, "All Products are already Synced", null);
                var unifiedItems = ProductMapper.MapQuickBooksProductsToUnifiedItems(allProducts);
                return new CommonResponse<object>(200, "Products fetched and mapped successfully", unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing products", ex.Message);
            }
        }



        public async Task<CommonResponse<object>> AddItem(ConnectionModal connection, QuickBooksProductInputModel input)
        {
            try
            {
                object itemPayload;

                if (input.Type.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
                {
                    itemPayload = new
                    {
                        Name = input.Name,
                        Active = input.IsActive,
                        Type = "Inventory",
                        Description = input.Description,
                        UnitPrice = input.SalesUnitPrice,
                        PurchaseCost = input.PurchaseUnitPrice,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" },
                        ExpenseAccountRef = new { value = "80", name = "Cost of Goods Sold" },
                        AssetAccountRef = new { value = "81", name = "Inventory Asset" },
                        TrackQtyOnHand = true,
                        QtyOnHand = input.QtyOnHand,
                        InvStartDate = input.InvStartDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")
                    };
                }
                else
                {
                    itemPayload = new
                    {
                        Name = input.Name,
                        Active = input.IsActive,
                        Type = "Service",
                        Description = input.Description,
                        UnitPrice = input.SalesUnitPrice,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" }
                    };
                }

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("item", itemPayload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = apiResponse.Data;
                    var final = JsonConvert.DeserializeObject<QuickBooksProductResponse>(apiResponse.Data.ToString());
                    var product = final?.Item;

                    if (product != null)
                    {
                        var productList = new List<QuickBooksProduct> { product };
                        var unifiedItems = ProductMapper.MapQuickBooksProductsToUnifiedItems(productList);
                        return new CommonResponse<object>(200, "Product added successfully", unifiedItems);
                    }

                    return new CommonResponse<object>(200, "Product added but could not parse product details", null);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to add product", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding product", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditItem(ConnectionModal connection, string itemId,QuickBooksProductInputModel input)
        {
            try
            {
                var syncTokenResult = await GetItemById(connection, itemId);
                if (syncTokenResult.Status != 200 || syncTokenResult.Data == null)
                {
                    return new CommonResponse<object>(syncTokenResult.Status, "Failed to fetch SyncToken", syncTokenResult.Data);
                }

                var existingProduct = (QuickBooksProduct)syncTokenResult.Data;
                object itemPayload;

                if (input.Type.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
                {
                    itemPayload = new
                    {
                        Id = itemId,
                        SyncToken = existingProduct.SyncToken,
                        Name = input.Name,
                        Active = input.IsActive,
                        Type = "Inventory",
                        Description = input.Description,
                        UnitPrice = input.SalesUnitPrice,
                        PurchaseCost = input.PurchaseUnitPrice,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" },
                        ExpenseAccountRef = new { value = "80", name = "Cost of Goods Sold" },
                        AssetAccountRef = new { value = "81", name = "Inventory Asset" },
                        TrackQtyOnHand = true,
                        QtyOnHand = input.QtyOnHand,
                        InvStartDate = input.InvStartDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")
                    };
                }
                else
                {
                    itemPayload = new
                    {
                        Id = itemId,
                        SyncToken = existingProduct.SyncToken,
                        Name = input.Name,
                        Active = input.IsActive,
                        Type = "Service",
                        Description = input.Description,
                        UnitPrice = input.SalesUnitPrice,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" }
                    };
                }

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("item", itemPayload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksProductResponse>(apiResponse.Data.ToString());
                    var unifiedItems = ProductMapper.MapQuickBooksProductsToUnifiedItems(new List<QuickBooksProduct> { result.Item });
                    return new CommonResponse<object>(200, "Product updated successfully", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update product", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating product", ex.Message);
            }
        }



        public async Task<CommonResponse<object>> GetItemById(ConnectionModal connection, string itemId)
        {
            try
            {
                var apiResponse = await _quickBooksApiService.QuickBooksGetRequest($"item/{itemId}", connection);

                if (apiResponse.Status == 200)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksProductResponse>(apiResponse.Data.ToString());
                    return new CommonResponse<object>(200, "Item fetched successfully", result.Item);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to fetch item", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while fetching item", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditQuickBookItemStatusAsync(ConnectionModal connection, string itemId, bool status)
        {
            try
            {
                var syncTokenResult = await GetItemById(connection, itemId);
                if (syncTokenResult.Status != 200 || syncTokenResult.Data == null)
                {
                    return new CommonResponse<object>(syncTokenResult.Status, "Failed to fetch item for SyncToken", syncTokenResult.Data);
                }

                var existingProduct = (QuickBooksProduct)syncTokenResult.Data;
                object itemPayload;

                if (existingProduct.Type.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
                {
                    itemPayload = new
                    {
                        Id = itemId,
                        SyncToken = existingProduct.SyncToken,
                        Name = existingProduct.Name,
                        Type = "Inventory",
                        Active = status,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" },
                        ExpenseAccountRef = new { value = "80", name = "Cost of Goods Sold" },
                        AssetAccountRef = new { value = "81", name = "Inventory Asset" },
                        TrackQtyOnHand = true,
                        QtyOnHand = existingProduct.QtyOnHand,
                    };
                }
                else
                {
                    itemPayload = new
                    {
                        Id = itemId,
                        SyncToken = existingProduct.SyncToken,
                        Name = existingProduct.Name,
                        Type = "Service",
                        Active = status,
                        IncomeAccountRef = new { value = "79", name = "Sales of Product Income" }
                    };
                }

                var apiResponse = await _quickBooksApiService.QuickBooksPostRequest("item", itemPayload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var result = JsonConvert.DeserializeObject<QuickBooksProductResponse>(apiResponse.Data.ToString());
                    var unifiedItems = ProductMapper.MapQuickBooksProductsToUnifiedItems(new List<QuickBooksProduct> { result.Item });
                    return new CommonResponse<object>(200, "Product status updated successfully", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update product status", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating product status", ex.Message);
            }
        }
        public class QuickBooksProductResponse
        {
            public QuickBooksProduct Item { get; set; }
        }
    }
    }
