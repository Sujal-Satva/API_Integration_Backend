using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using SharedModels.Models;
using task_14.Data;
using task_14.Services;

namespace task_14.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        
        private readonly IInvoiceRepository _invoiceRepository;
     
        public InvoiceController(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }


        [HttpGet("fetch-qbo")]
        public async Task<IActionResult> FetchItemsFromQuickBooks()
        {
            try
            {
                var response = await _invoiceRepository.SyncInvoices("QuickBooks");

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
                var response = await _invoiceRepository.SyncInvoices("Xero");
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
        public async Task<IActionResult> AddInvoice([FromQuery] string platform, [FromBody] object input)
        {
            try
            {
                if (string.IsNullOrEmpty(platform))
                    return BadRequest(new CommonResponse<object>(400, "Platform is required (xero or quickbooks)"));

                var response = await _invoiceRepository.AddInvoicesAsync(platform, input);

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

        [HttpPut("edit")]
        public async Task<IActionResult> EditInvoice([FromQuery] string platform,[FromQuery] string id,[FromBody] object input)
        {
            try
            {
                if (string.IsNullOrEmpty(platform))
                    return BadRequest(new CommonResponse<object>(400, "Platform is required (xero or quickbooks)"));

                var response = await _invoiceRepository.EditInvoicesAsync(platform, id,input);

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

        [HttpDelete("delete")]

        public async Task<IActionResult> DeleteInvoice([FromQuery] string platform, [FromQuery] string id, [FromQuery] string status)
        {
            try
            {
                if (string.IsNullOrEmpty(platform))
                    return BadRequest(new CommonResponse<object>(400, "Platform is required (xero or quickbooks)"));

                var response = await _invoiceRepository.DeleteInvoiceAsync(platform, id, status);

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

        [HttpGet]
        public async Task<IActionResult> GetInvoices(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] bool pagination = true,
            [FromQuery] string? source = "all",
            [FromQuery] bool isBill=false)
        {
            try
            {
                var result = await _invoiceRepository.GetInvoicesAsync(page, pageSize, search, sortBy, sortDirection, pagination, source,isBill);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error fetching invoices",
                    error = ex.Message,
                    trace = ex.StackTrace
                });
            }
        }


        //[HttpPost("upload-csv")]
        //public async Task<IActionResult> HandleCSV(IFormFile file)
        //{
        //    try
        //    {
        //        var tokenResult = await _tokenRepository.GetTokenAsync();
        //        if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
        //        {
        //            return Unauthorized(new ApiResponse<string>(
        //                error: "TokenInvalid",
        //                message: "QuickBooks access token is invalid or expired."
        //            ));
        //        }

        //        var result = await _invoiceRepository.ProcessCsvFileAsync(tokenResult.Token, tokenResult.RealmId, file);
        //        if (result.Error != null)
        //        {
        //            return StatusCode(500, new ApiResponse<object>
        //            {
        //                Error = result.Error,
        //                Message = result.Message,
        //                Data = result.Data
        //            });
        //        }

        //        return Ok(new ApiResponse<object>
        //        {
        //            Message = "CSV file processed successfully.",
        //            Data = result.Data
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new ApiResponse<object>
        //        {
        //            Error = "UnexpectedError",
        //            Message = "An unexpected error occurred while processing the CSV file.",
        //            Data = new { ErrorMessage = ex.Message, Trace = ex.StackTrace }
        //        });
        //    }
        //}

    }
}
