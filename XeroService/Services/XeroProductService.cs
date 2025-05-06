
using Newtonsoft.Json;
using XeroService.Interfaces;
using QuickBookService.Mappers;
using SharedModels.QuickBooks.Models;
using System;
using System.Collections.Generic;
using SharedModels.Models;
using SharedModels.Xero.Models;
using System.Threading.Tasks;


namespace QuickBookService.Services
{
    public class XeroProductService:IXeroProductService
    {
        private readonly IXeroApiService _xeroApiService;
      
        public XeroProductService(IXeroApiService xeroApiService)
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


        public async Task<CommonResponse<object>> FetchProductsFromXero(ConnectionModal connection)
        {
            try
            {
                DateTime lastSynced = GetLastSyncedDate(connection.SyncingInfo, "Products");
                DateTime bufferedTime = lastSynced.ToUniversalTime().AddMinutes(-10);
                string dateFilter = bufferedTime > new DateTime(2000, 1, 1)
                    ? $"?where=UpdatedDateUTC>=DateTime({bufferedTime:yyyy, MM, dd, HH, mm, ss})"
                    : string.Empty;
                int page = 1;
                var allProducts = new List<XeroProduct>();

                while (true)
                {
                    var apiResponse = await _xeroApiService.XeroGetRequest($"/Items{dateFilter}&page={page}", connection);
                    if (apiResponse.Status != 200 || apiResponse.Data == null)
                        return new CommonResponse<object>(apiResponse.Status, "Failed to fetch products from Xero", apiResponse.Data);
                    var final = JsonConvert.DeserializeObject<XeroProductResponse>(apiResponse.Data.ToString());
                    var products = final?.Items;
                    if (products == null || products.Count == 0) break;
                    allProducts.AddRange(products);
                    if (products.Count < 100) 
                        break;
                    page++;
                }
                if (allProducts.Count == 0)
                    return new CommonResponse<object>(200, "All Products are already Synced", null);
                var unifiedItems = ProductMapper.MapXeroProductsToUnifiedItems(allProducts);
                return new CommonResponse<object>(200, "Products fetched and mapped successfully", unifiedItems);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while syncing products from Xero", ex.Message);
            }
        }


        public async Task<CommonResponse<object>> AddItem(ConnectionModal connection, XeroProductInputModel input)
        {
            try
            {
                var itemPayload = new
                {
                    Code = input.Code,
                    Name = input.Name,
                    Description = input.Description,
                    PurchaseDescription = input.PurchaseDescription,
                    IsSold = input.IsSold,
                    IsPurchased = input.IsPurchased,
                    SalesDetails = input.IsSold ? new
                    {
                        UnitPrice = input.SalesUnitPrice,
                        AccountCode = input.SalesAccountCode,
                        TaxType = input.SalesTaxType
                    } : null,
                    PurchaseDetails = input.IsPurchased ? new
                    {
                        UnitPrice = input.PurchaseUnitPrice,
                        COGSAccountCode = input.PurchaseAccountCode,
                        TaxType = input.PurchaseTaxType
                    } : null,
                    IsTrackedAsInventory = input.IsTrackedAsInventory,
                    InventoryAssetAccountCode = input.IsTrackedAsInventory ? input.InventoryAssetAccountCode : null
                };
                var apiResponse = await _xeroApiService.XeroPostRequest("/Items", itemPayload, connection);

                if (apiResponse.Status == 200 || apiResponse.Status == 201)
                {
                    var final = JsonConvert.DeserializeObject<XeroProductResponse>(apiResponse.Data.ToString());
                    var products = final?.Items;
                    
                    var unifiedItems = ProductMapper.MapXeroProductsToUnifiedItems(products);
                    
                    return new CommonResponse<object>(200, "Product added successfully to Xero", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to add product to Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while adding product to Xero", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditItem(ConnectionModal connection, string itemId, XeroProductInputModel input)
        {
            try
            {
                var itemPayload = new
                {
                    Code = input.Code,
                    Name = input.Name,
                    Description = input.Description,
                    PurchaseDescription = input.PurchaseDescription,
                    IsSold = input.IsSold,
                    IsPurchased = input.IsPurchased,
                    SalesDetails = input.IsSold ? new
                    {
                        UnitPrice = input.SalesUnitPrice,
                        AccountCode = input.SalesAccountCode,
                        TaxType = input.SalesTaxType
                    } : null,
                    PurchaseDetails = input.IsPurchased ? new
                    {
                        UnitPrice = input.PurchaseUnitPrice,
                        COGSAccountCode = input.PurchaseAccountCode,
                        TaxType = input.PurchaseTaxType
                    } : null,
                    IsTrackedAsInventory = input.IsTrackedAsInventory,
                    InventoryAssetAccountCode = input.IsTrackedAsInventory ? input.InventoryAssetAccountCode : null
                };

                var apiResponse = await _xeroApiService.XeroPostRequest($"/Items/{itemId}", itemPayload, connection);

                if (apiResponse.Status == 200)
                {
                    var final = JsonConvert.DeserializeObject<XeroProductResponse>(apiResponse.Data.ToString());
                    var products = final?.Items;
                  
                    var unifiedItems = ProductMapper.MapXeroProductsToUnifiedItems(products);
                    return new CommonResponse<object>(200, "Product updated successfully in Xero", unifiedItems);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to update product in Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Error occurred while updating product in Xero", ex.Message);
            }
        }

        public async Task<CommonResponse<object>> EditXeroItemStatusAsync(ConnectionModal connection, string id, bool status)
        {
            try
            {
                var apiResponse = await _xeroApiService.XeroDeleteRequest($"/Items/{id}", connection);

                if (apiResponse.Status == 200)
                {
              
                    return new CommonResponse<object>(200, "Item deleted successfully from Xero", null);
                }

                return new CommonResponse<object>(apiResponse.Status, "Failed to delete item from Xero", apiResponse.Data);
            }
            catch (Exception ex)
            {
                return new CommonResponse<object>(500, "Exception occurred while deleting item from Xero", ex.Message);
            }
        }

        //public async Task<CommonResponse<object>> EditXeroItemStatusAsync(ConnectionModal connection, string id, bool status)
        //{
        //    try
        //    {
        //        var getResponse = await _xeroApiService.XeroGetRequest($"/Items/{id}", connection);

        //        if (getResponse.Status != 200 || getResponse.Data == null)
        //        {
        //            return new CommonResponse<object>(getResponse.Status, "Failed to retrieve item from Xero", getResponse.Data);
        //        }
        //        var itemResponse = JsonConvert.DeserializeObject<XeroProductResponse>(JsonConvert.SerializeObject(getResponse.Data));
        //        var item = itemResponse?.Items?.FirstOrDefault();

        //        if (item == null || string.IsNullOrEmpty(item.ItemId) || string.IsNullOrEmpty(item.Code))
        //        {
        //            return new CommonResponse<object>(400, "Invalid item data from Xero", null);
        //        }
        //        var payload = new
        //        {
        //            Items = new[]
        //            {
        //                    new
        //                    {
        //                        Code = item.Code,
        //                        Name = item.Name,
        //                        ItemStatus = status ? "ACTIVE" : "ARCHIVED"
        //                    }
        //            }
        //        };
        //        var putResponse = await _xeroApiService.XeroPostRequest($"/Items/{item.ItemId}", payload, connection);
        //        if (putResponse.Status == 200)
        //        {
        //            return new CommonResponse<object>(200, $"Item {(status ? "activated" : "archived")} successfully in Xero", putResponse.Data);
        //        }
        //        return new CommonResponse<object>(putResponse.Status, "Failed to update item status in Xero", putResponse.Data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CommonResponse<object>(500, "Exception occurred while updating item status in Xero", ex.Message);
        //    }
        //}

    }
}
