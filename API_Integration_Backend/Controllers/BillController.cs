using Microsoft.AspNetCore.Mvc;
using SharedModels.Models;
using task_14.Services;

namespace task_14.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly IBillRepository _billRepository;
        public BillsController( IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }
        [HttpGet("fetch-qbo")]
        public async Task<IActionResult> FetchBillsFromQuickBooks()
        {
            try
            {
                var response = await _billRepository.SyncBills("QuickBooks");

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
        public async Task<IActionResult> FetchBillsFromXero()
        {
            try
            {
                var response = await _billRepository.SyncBills("Xero");
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



        [HttpGet]
        public async Task<IActionResult> GetBills(
           int page = 1,
           int pageSize = 10,
           string? search = null,
           string? sortColumn = "",
           string? sortDirection = "desc",
           bool pagination = true,
           string sourceSystem="all")
        {
            var result = await _billRepository.GetBillsAsync(
                page, pageSize, search, sortColumn, sortDirection, pagination,
                sourceSystem
            );

            return Ok(result);
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddBill([FromQuery] string platform, [FromBody] object input)
        {
            try
            {
                if (string.IsNullOrEmpty(platform))
                    return BadRequest(new CommonResponse<object>(400, "Platform is required (xero or quickbooks)"));

                var response = await _billRepository.AddBillsAsync(platform, input);

                if (response.Status != 200)
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
    }
}
