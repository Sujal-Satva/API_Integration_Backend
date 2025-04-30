using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http.Headers;
using task_14.Data;
using task_14.Models;
using task_14.Repository;
using task_14.Services;

namespace task_14.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly IBillRepository _billRepository;
        private readonly ITokenRespository _tokenRepository;
        public BillsController( IBillRepository billRepository, ITokenRespository tokenRespository)
        {
            _billRepository = billRepository;
            _tokenRepository = tokenRespository;
        }

        [HttpGet("fetch")]
        public async Task<ActionResult<ApiResponse<string>>> SyncBillsFromQuickBooks()
        {
            try
            {
                var tokenResult = await _tokenRepository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<string>(error: "TokenInvalid", message: "QuickBooks access token is invalid or expired."));
                }

                var result = await _billRepository.SyncBillsFromQuickBooksAsync(tokenResult.Token, tokenResult.RealmId);

               
                if (result.Error != null)
                {
                    return BadRequest(result); 
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new ApiResponse<string>(
                    error: ex.Message,
                    message: "An unexpected error occurred while syncing bills from QuickBooks.",
                    data: ex.StackTrace
                ));
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetBills(
           int page = 1,
           int pageSize = 10,
           string? search = null,
           string? sortColumn = null,
           string? sortDirection = "asc",
           bool pagination = true)
        {
            var result = await _billRepository.GetBillsAsync(
                page, pageSize, search, sortColumn, sortDirection, pagination
            );

            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> CreateBill([FromBody] QuickBooksBillCreateDto billDto)
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

                var result = await _billRepository.CreateBillAsync(billDto, tokenResult.Token, tokenResult.RealmId);

              
                if (!string.IsNullOrEmpty(result.Error))
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new ApiResponse<string>(
                    error: ex.Message,
                    message: "An unexpected error occurred while creating the bill.",
                    data: ex.StackTrace 
                ));
            }
        }

    }
}
