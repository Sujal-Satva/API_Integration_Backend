using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedModels.Models;
using SharedModels.QuickBooks.Models;
using task_14.Data;

using task_14.Services;


namespace task_14.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        
        private readonly IVendorRepository _vendorRepository;


        public VendorController(ApplicationDbContext context,IVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
          
        }

        [HttpGet("fetch-qbo")]
        public async Task<IActionResult> FetchItemsFromQuickBooks()
        {
            try
            {
                var response = await _vendorRepository.SyncVendors("QuickBooks");

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

        //[HttpGet("fetch")]
        //public async Task<IActionResult> SyncVendorsFromQuickBooks()
        //{
        //    try
        //    {
        //        var tokenResult = await _tokenRespository.GetTokenAsync();
        //        if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
        //        {
        //            return Unauthorized(new ApiResponse<List<Vendor>>(
        //                error: "Token invalid or expired",
        //                message: "Could not retrieve a valid QuickBooks access token."
        //            ));
        //        }

        //        var vendors = await _vendorRepository.SyncVendorsFromQuickBooksAsync(tokenResult.Token, tokenResult.RealmId);

        //        if (!vendors.Any())
        //        {
        //            return Ok(new ApiResponse<List<Vendor>>([], message: "No new vendors found to sync."));
        //        }

        //        return Ok(new ApiResponse<List<Vendor>>(vendors, message: "Vendors synced successfully."));
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new ApiResponse<List<Vendor>>(
        //            null,
        //            error: ex.Message,
        //            message: "An error occurred while syncing vendors."
        //        ));
        //    }
        //}

        [HttpGet("all")]
        public async Task<IActionResult> GetAllActiveVendorsAsync(
            string? search,
            string? sortColumn,
            string? sortDirection,
            bool pagination = true,
            int page = 1,
            int pageSize = 10, bool active = true)
        {
            try
            {
                var result = await _vendorRepository.GetAllActiveVendorsAsync(search, sortColumn, sortDirection, pagination, page, pageSize, active);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddCustomer([FromBody] VendorInputModel inputModel, [FromQuery] string platform)
        {
            var response = await _vendorRepository.AddCustomersAsync(platform, inputModel);
            return StatusCode(response.Status, response);
        }

        [HttpPut("edit")]
        public async Task<IActionResult> EditCustomer([FromBody] VendorInputModel inputModel, [FromQuery] string id, [FromQuery] string platform)
        {
            var response = await _vendorRepository.EditCustomersAsync(platform, id, inputModel);
            return StatusCode(response.Status, response);
        }


        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromQuery] string id, [FromQuery] string platform, [FromQuery] string status)
        {
            var response = await _vendorRepository.UpdateCustomerStatusAsync(id, platform, status);
            return StatusCode(response.Status, response);
        }


    }
}
