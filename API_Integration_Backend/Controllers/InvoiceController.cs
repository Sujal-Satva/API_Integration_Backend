using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Formats.Asn1;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using task_14.Data;
using task_14.Models;
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


        [HttpGet("fetch")]
        public async Task<IActionResult> GetInvoiceFromQuickBooks()
        {
            try
            {
                var tokenResult = await _tokenRepository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<object>(
                        error: "TokenInvalid",
                        message: "QuickBooks access token is invalid or expired."
                    ));
                }

                var result = await _invoiceRepository.SyncInvoicesFromQuickBooksAsync(tokenResult.Token, tokenResult.RealmId);

                var response = new ApiResponse<object>
                {
                    Data = result.Data,
                    Message = result.Message
                };

                if (!string.IsNullOrEmpty(result.Error))
                {
                    response.Error = result.Error;
                    return result.Error == "NoInvoicesFound"
                        ? BadRequest(response)
                        : StatusCode(500, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Error = "ErrorSyncingInvoices",
                    Message = "An error occurred while syncing invoices.",
                    Data = new { ErrorMessage = ex.Message, Trace = ex.StackTrace }
                });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetInvoices(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] bool pagination = true)
        {
            try
            {
                var result = await _invoiceRepository.GetInvoicesAsync(page, pageSize, search, sortBy, sortDirection, pagination);
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

        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceInputModel model)
        {
            try
            {
                var tokenResult = await _tokenRepository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<object>(
                        error: "TokenInvalid",
                        message: "QuickBooks access token is invalid or expired."
                    ));
                }

                var result = await _invoiceRepository.CreateInvoiceAsync(tokenResult.Token, model, tokenResult.RealmId);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    return StatusCode(500, new ApiResponse<object>(
                        error: result.Error,
                        message: "Failed to create invoice"
                    ));
                }

                return Ok(new ApiResponse<object>(
                    data: result.Data,
                    message: "Invoice created successfully"
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(
                    error: ex.Message,
                    message: "Unexpected error while creating invoice",
                    data: new { ex.StackTrace }
                ));
            }
        }



        [HttpPut("{invoiceId}")]
        public async Task<IActionResult> UpdateInvoice(int invoiceId,[FromBody] InvoiceInputModel model)
        {
            try
            {
               
                var tokenResult = await _tokenRepository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<object>(
                        error: "TokenInvalid",
                        message: "QuickBooks access token is invalid or expired."
                    ));
                }

             
                var result = await _invoiceRepository.UpdateInvoiceAsync(tokenResult.Token, invoiceId, model, tokenResult.RealmId);
                if (!string.IsNullOrEmpty(result.Error))
                {
                    return StatusCode(500, new ApiResponse<object>(
                        error: result.Error,
                        message: "Failed to update invoice"
                    ));
                }
                return Ok(new ApiResponse<object>(
                    data: result.Data,
                    message: "Invoice updated successfully"
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(
                    error: ex.Message,
                    message: "Unexpected error while updating invoice",
                    data: new { ex.StackTrace }
                ));
            }
        }

        [HttpDelete("{invoiceId}")]
        public async Task<IActionResult> DeleteInvoice(int invoiceId)
        {
            try
            {
                
                var tokenResult = await _tokenRepository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<string>(
                        error: "TokenInvalid",
                        message: "QuickBooks access token is invalid or expired."
                    ));
                }

               
                var result = await _invoiceRepository.DeleteInvoiceAsync(tokenResult.Token, invoiceId, tokenResult.RealmId);

                
                if (result.Error != null)
                {
                    if (result.Error == "NotFound")
                        return NotFound(new ApiResponse<string> { Message = result.Message });

                    return StatusCode(500, new ApiResponse<string>
                    {
                        Error = result.Error,
                        Message = result.Message
                    });
                }
                return Ok(new ApiResponse<string>
                {
                    Message = result.Message,
                    Data = "Invoice Delete Sucessfully"
                });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new ApiResponse<string>
                {
                    Error = ex.Message,
                    Message = "Delete failed",
                    Data = ex.StackTrace
                });
            }
        }



        [HttpPost("upload-csv")]
        public async Task<IActionResult> HandleCSV(IFormFile file)
        {
            try
            {
                var tokenResult = await _tokenRepository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<string>(
                        error: "TokenInvalid",
                        message: "QuickBooks access token is invalid or expired."
                    ));
                }

                var result = await _invoiceRepository.ProcessCsvFileAsync(tokenResult.Token, tokenResult.RealmId, file);
                if (result.Error != null)
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Error = result.Error,
                        Message = result.Message,
                        Data = result.Data
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Message = "CSV file processed successfully.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Error = "UnexpectedError",
                    Message = "An unexpected error occurred while processing the CSV file.",
                    Data = new { ErrorMessage = ex.Message, Trace = ex.StackTrace }
                });
            }
        }

    }
}
