
using Microsoft.AspNetCore.Mvc;
using SharedModels.Models;
using task_14.Data;
using task_14.Services;

namespace task_13.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductRepository _productRepository;
        

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }


        [HttpGet("fetch-qbo")]
        public async Task<IActionResult> FetchItemsFromQuickBooks()
        {
            try
            {
                var response = await _productRepository.SyncItems("QuickBooks");

                return StatusCode(response.Status, response); 
            }
            catch (Exception ex)
            {
                var errorResponse = new CommonResponse<object>(
                    500,
                    "Internal server error",
                    ex.Message
                );

                return StatusCode(500, errorResponse);
            }
        }


        [HttpGet("fetch-xero")]
        public async Task<IActionResult> FetchItemsFromXero()
        {
            try
            {
                var response = await _productRepository.SyncItems("Xero");
                return StatusCode(response.Status, response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CommonResponse<object>(
                    500,
                    "Internal server error",
                    ex.Message
                );

                return StatusCode(500, errorResponse);
            }
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddItems([FromQuery] string platform, [FromBody] object input)
        {
            try
            {
                if (string.IsNullOrEmpty(platform))
                    return BadRequest(new CommonResponse<object>(400, "Platform is required (xero or quickbooks)"));

                var response = await _productRepository.AddItemsAsync(platform, input);

                if (response.Status!=200)
                {
                    return BadRequest(new CommonResponse<object>(response.Status,response.Message,response.Data));
                }

                return StatusCode(response.Status, response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CommonResponse<object>(
                    500,
                    "Internal server error",
                    ex.Message
                );

                return StatusCode(500, errorResponse);
            }
        }

        [HttpPut("edit")]
        public async Task<IActionResult> EditItems([FromQuery] string platform, [FromQuery] string itemId, [FromBody] object input)
        {
            try
            {
                if (string.IsNullOrEmpty(platform))
                    return BadRequest(new CommonResponse<object>(400, "Platform is required (xero or quickbooks)"));

                if (string.IsNullOrEmpty(itemId))
                    return BadRequest(new CommonResponse<object>(400, "Item ID is required"));

                var response = await _productRepository.EditItemsAsync(platform, itemId, input);

                if (response != null && response.Status != 200)
                {
                    return BadRequest(new CommonResponse<object>(response.Status, response.Message, response.Data));
                }

                return StatusCode(response.Status, response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CommonResponse<object>(
                    500,
                    "Internal server error",
                    ex.Message
                );

                return StatusCode(500, errorResponse);
            }
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetItems(string search = "",
                    string sortColumn = "name",
                    string sortDirection = "asc",
                    int page = 1,
                    int pageSize = 10,
                    bool pagination = true,
                    bool active = true,
                    string sourceSystem = "All",
                    string sourceType="All")
        {
            try
            {
                var response = await _productRepository.GetItems(search, sortColumn, sortDirection, page, pageSize, pagination, active, sourceSystem,sourceType);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CommonResponse<object>(
                    500,
                    "Internal server error",
                    ex.Message
                );

                return StatusCode(500, errorResponse);
            }
        }

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateItemStatus(
            [FromQuery] string id,
            [FromQuery] bool status,
            [FromQuery] string platform)
        {
            try
            {
                var response = await _productRepository.EditItemStatusAsync(id, status, platform);
                if (response != null && response.Status != 200)
                {
                    return BadRequest(new CommonResponse<object>(response.Status, response.Message, response.Data));
                }

                return StatusCode(response.Status, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonResponse<object>(
                    500,
                    "Internal server error",
                    ex.Message
                ));
            }
        }

    }
}
