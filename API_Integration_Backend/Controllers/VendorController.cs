using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using task_14.Data;
using task_14.Models;
using task_14.Repository;
using task_14.Services;
using static task_14.Controllers.VendorController;

namespace task_14.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorRepository _vendorRepository;
        private readonly ITokenRespository _tokenRespository;
        private readonly IHttpClientFactory _httpClientFactory;

        public VendorController(ApplicationDbContext context, IHttpClientFactory httpClientFactory,IVendorRepository vendorRepository,ITokenRespository tokenRespository)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _vendorRepository = vendorRepository;
            _tokenRespository = tokenRespository;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> SyncVendorsFromQuickBooks()
        {
            try
            {
                var tokenResult = await _tokenRespository.GetTokenAsync();
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Token) || string.IsNullOrEmpty(tokenResult.RealmId))
                {
                    return Unauthorized(new ApiResponse<List<Vendor>>(
                        error: "Token invalid or expired",
                        message: "Could not retrieve a valid QuickBooks access token."
                    ));
                }

                var vendors = await _vendorRepository.SyncVendorsFromQuickBooksAsync(tokenResult.Token, tokenResult.RealmId);

                if (!vendors.Any())
                {
                    return Ok(new ApiResponse<List<Vendor>>([], message: "No new vendors found to sync."));
                }

                return Ok(new ApiResponse<List<Vendor>>(vendors, message: "Vendors synced successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<Vendor>>(
                    null,
                    error: ex.Message,
                    message: "An error occurred while syncing vendors."
                ));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllActiveVendorsAsync(
            string? search,
            string? sortColumn,
            string? sortDirection,
            bool pagination = true,
            int page = 1,
            int pageSize = 10,bool active=true)
        {
            try
            {
                var result = await _vendorRepository.GetAllActiveVendorsAsync(search, sortColumn, sortDirection, pagination, page, pageSize,active);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new PagedResponse<Vendor>(new List<Vendor>(), page, pageSize, 0));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }



        [HttpPost("add")]
        public async Task<ActionResult<ApiResponse<string>>> AddVendorAsync(
            [FromBody] VendorInputModal inputModel)
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

                var result = await _vendorRepository.AddVendorAsync(inputModel, tokenResult.Token, tokenResult.RealmId);

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
                    message: "Unexpected error while adding vendor."
                ));
            }
        }

        [HttpPut("update/{vendorId}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateVendorAsync(
            string vendorId,
            [FromBody] VendorInputModal inputModel)
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

                var response = await _vendorRepository.UpdateVendorAsync(vendorId, inputModel, tokenResult.Token, tokenResult.RealmId);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(
                    error: ex.Message,
                    message: "Unexpected error while updating vendor."
                ));
            }
        }

        [HttpDelete("delete/{vendorId}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteVendorAsync(
            string vendorId)
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

                var response = await _vendorRepository.DeactivateVendorAsync(vendorId, tokenResult.Token, tokenResult.RealmId);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    return BadRequest(response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(
                    error: ex.Message,
                    message: "Unexpected error while deactivating vendor."
                ));
            }
        }


        [HttpPut("{vendorId}/activate")]
        public async Task<IActionResult> ActivateVendor([FromRoute] string vendorId)
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

                var result = await _vendorRepository.ActivateVendorAsync(vendorId, tokenResult.Token, tokenResult.RealmId);

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
                    message: "Unexpected error while activating vendor."
                ));
            }
        }



    }
}
