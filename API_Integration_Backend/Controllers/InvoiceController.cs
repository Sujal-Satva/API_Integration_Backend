using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedModels.Models;
using System.Formats.Asn1;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using task_14.Data;
using task_14.Models;
using task_14.Repository;
using task_14.Services;

namespace task_14.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ApplicationDbContext _context;
        private readonly ITokenRespository _tokenRepository;
      
        
        public InvoiceController(IConfiguration configuration, ApplicationDbContext context, IInvoiceRepository invoiceRepository,ITokenRespository tokenRespository)
        {
            _configuration = configuration;
            _invoiceRepository = invoiceRepository;
            _tokenRepository = tokenRespository;
            _context = context;
           
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
            [FromQuery] string? source = "all")
        {
            try
            {
                var result = await _invoiceRepository.GetInvoicesAsync(page, pageSize, search, sortBy, sortDirection, pagination, source);
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

        //[HttpPost]
        //public async Task<IActionResult> CreateInvoice([FromBody] InvoiceInputModel model)
        //{
        //    try
        //    {
        //        var tokenResult = await _tokenRepository.GetTokenAsync();
        //        if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
        //        {
        //            return Unauthorized(new ApiResponse<object>(
        //                error: "TokenInvalid",
        //                message: "QuickBooks access token is invalid or expired."
        //            ));
        //        }

        //        var result = await _invoiceRepository.CreateInvoiceAsync(tokenResult.Token, model, tokenResult.RealmId);

        //        if (!string.IsNullOrEmpty(result.Error))
        //        {
        //            return StatusCode(500, new ApiResponse<object>(
        //                error: result.Error,
        //                message: "Failed to create invoice"
        //            ));
        //        }

        //        return Ok(new ApiResponse<object>(
        //            data: result.Data,
        //            message: "Invoice created successfully"
        //        ));
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new ApiResponse<object>(
        //            error: ex.Message,
        //            message: "Unexpected error while creating invoice",
        //            data: new { ex.StackTrace }
        //        ));
        //    }
        //}



        //[HttpPut("{invoiceId}")]
        //public async Task<IActionResult> UpdateInvoice(int invoiceId,[FromBody] InvoiceInputModel model)
        //{
        //    try
        //    {

        //        var tokenResult = await _tokenRepository.GetTokenAsync();
        //        if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
        //        {
        //            return Unauthorized(new ApiResponse<object>(
        //                error: "TokenInvalid",
        //                message: "QuickBooks access token is invalid or expired."
        //            ));
        //        }


        //        var result = await _invoiceRepository.UpdateInvoiceAsync(tokenResult.Token, invoiceId, model, tokenResult.RealmId);
        //        if (!string.IsNullOrEmpty(result.Error))
        //        {
        //            return StatusCode(500, new ApiResponse<object>(
        //                error: result.Error,
        //                message: "Failed to update invoice"
        //            ));
        //        }
        //        return Ok(new ApiResponse<object>(
        //            data: result.Data,
        //            message: "Invoice updated successfully"
        //        ));
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new ApiResponse<object>(
        //            error: ex.Message,
        //            message: "Unexpected error while updating invoice",
        //            data: new { ex.StackTrace }
        //        ));
        //    }
        //}

        //[HttpDelete("{invoiceId}")]
        //public async Task<IActionResult> DeleteInvoice(int invoiceId)
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


        //        var result = await _invoiceRepository.DeleteInvoiceAsync(tokenResult.Token, invoiceId, tokenResult.RealmId);


        //        if (result.Error != null)
        //        {
        //            if (result.Error == "NotFound")
        //                return NotFound(new ApiResponse<string> { Message = result.Message });

        //            return StatusCode(500, new ApiResponse<string>
        //            {
        //                Error = result.Error,
        //                Message = result.Message
        //            });
        //        }
        //        return Ok(new ApiResponse<string>
        //        {
        //            Message = result.Message,
        //            Data = "Invoice Delete Sucessfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {

        //        return StatusCode(500, new ApiResponse<string>
        //        {
        //            Error = ex.Message,
        //            Message = "Delete failed",
        //            Data = ex.StackTrace
        //        });
        //    }
        //}



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
