using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using task_14.Data;
using task_14.Models;
using task_14.Repository;
using task_14.Services;

namespace task_13.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly ITokenRespository _tokenRespository;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductController(IProductRepository productRepository, ApplicationDbContext context, IHttpClientFactory httpClientFactory, ITokenRespository tokenRespository)
        {
            _productRepository = productRepository;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _tokenRespository = tokenRespository;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> SyncItemsFromQuickBooks()
        {
            var tokenResult = await _tokenRespository.GetTokenAsync();
            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
            {
                return Unauthorized(new ApiResponse<string>(
                    error: "Token invalid or expired",
                    message: "Could not retrieve a valid QuickBooks access token."
                ));
            }

            try
            {
                var success = await _productRepository.SyncItemsFromQuickBooksAsync(tokenResult.Token, tokenResult.RealmId);

                if (success)
                {
                    return Ok(new ApiResponse<string>(message: "Items synced successfully from QuickBooks."));
                }

                return BadRequest(new ApiResponse<string>(
                    error: "NoData",
                    message: "No items found in QuickBooks response."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(
                    error: ex.Message,
                    message: "Error syncing items from QuickBooks."
                ));
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetProducts(
                [FromQuery] int page,
                [FromQuery] int pageSize = 10,
                [FromQuery] string search = "",
                [FromQuery] string sortColumn = "Name",
                [FromQuery] string sortDirection = "asc",
                [FromQuery] bool pagination = true,
                [FromQuery] bool active=true)
        {
            try
            {

                var pagedProducts = await _productRepository.GetPagedProductsAsync(
                                                search, sortColumn, sortDirection, page, pageSize, pagination, active);

                return Ok(pagedProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching products", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(
            [FromBody] ProductInputModel inputModel)
        {
            var tokenResult = await _tokenRespository.GetTokenAsync();
            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
            {
                return Unauthorized(new ApiResponse<string>(
                    error: "Token invalid or expired",
                    message: "Could not retrieve a valid QuickBooks access token."
                ));
            }

            var result = await _productRepository.AddProductAsync(tokenResult.Token, tokenResult.RealmId, inputModel);

            if (result.Error != null)
            {
                return BadRequest(new ApiResponse<string>(
                    error: result.Error,
                    message: "Failed to add product to QuickBooks."
                ));
            }

            return Ok(new ApiResponse<object>(
                data: result.Data,
                message: "Product added successfully."
            ));
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromBody] ProductInputModel inputModel)
        {
            var tokenResult = await _tokenRespository.GetTokenAsync();
            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
            {
                return Unauthorized(new ApiResponse<string>(
                    error: "Token invalid or expired",
                    message: "Could not retrieve a valid QuickBooks access token."
                ));
            }

            var result = await _productRepository.UpdateProductAsync(tokenResult.Token, tokenResult.RealmId, id, inputModel);

            if (result.Error != null)
            {
                return BadRequest(new ApiResponse<string>(
                    error: result.Error,
                    message: "Failed to update product."
                ));
            }

            return Ok(new ApiResponse<object>(
                data: result.Data,
                message: "Product updated successfully."
            ));
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var tokenResult = await _tokenRespository.GetTokenAsync();
            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
            {
                return Unauthorized(new ApiResponse<string>(
                    error: "Token invalid or expired",
                    message: "Could not retrieve a valid QuickBooks access token."
                ));
            }

            var result = await _productRepository.DeleteProductAsync(tokenResult.Token, tokenResult.RealmId, id);

            if (result.Error != null)
            {
                return BadRequest(new ApiResponse<string>(
                    error: result.Error,
                    message: "Failed to delete product."
                ));
            }

            return Ok(new ApiResponse<object>(
                data: result.Data,
                message: "Product deleted successfully."
            ));
        }



        [HttpPut("markActive/{productId}")]
        public async Task<IActionResult> MarkProductActive([FromRoute] int productId)
        {
            try
            {
                var tokenResult = await _tokenRespository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<string>(
                        error: "Token invalid or expired",
                        message: "Could not retrieve a valid QuickBooks access token."
                    ));
                }

                var result = await _productRepository.MarkProductActiveAsync(tokenResult.Token, tokenResult.RealmId, productId);

                if (result.Error == null)
                {
                    return Ok(new ApiResponse<object>(
                        message: "Product marked as active successfully."
                    ));
                }

                return NotFound(new ApiResponse<string>(
                    error: result.Error,
                    message: "Product not found."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(
                    error: ex.Message,
                    message: "Failed to mark product as active."
                ));
            }
        }

    }
}
