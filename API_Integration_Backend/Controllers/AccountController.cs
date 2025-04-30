
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using task_14.Data;
using task_14.Models;
using task_14.Services;

namespace task_14.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnectionRepository _connectionRepository;
       
        private readonly IChartOfAccountRepository _chartOfAccountRepository;
        public AccountController(IConfiguration configuration, ApplicationDbContext context, IHttpClientFactory httpClientFactory,IChartOfAccountRepository chartOfAccountRepository, IConnectionRepository connectionRepository)
        {
            _configuration = configuration;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _chartOfAccountRepository = chartOfAccountRepository;
            _connectionRepository = connectionRepository;
        }

        [HttpGet("fetch-qbo")]
        public async Task<IActionResult> FetchAndSaveQBOChartOfAccounts()
        {
            var response = await _chartOfAccountRepository.FetchAndSaveQBOChartOfAccountsAsync();
            return StatusCode(response.Status, response);
        }

        [HttpGet("fetch-xero")]
        public async Task<IActionResult> FetchAndSaveXeroChartOfAccounts()
        {
            var response = await _chartOfAccountRepository.FetchAndSaveXeroChartOfAccountsAsync();
            return StatusCode(response.Status, response);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAccountsFromDb(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortColumn = "Name",
            [FromQuery] string? sortDirection = "asc",
            [FromQuery] bool pagination = true,
            [FromQuery] string? sourceSystem = null)
        {
            try
            {
                var response = await _chartOfAccountRepository.GetAccountsFromDbAsync(
                    page, pageSize, search, sortColumn, sortDirection, pagination, sourceSystem);

                return StatusCode(response.Status, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new CommonResponse<object>(400, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonResponse<object>(500, "An unexpected error occurred", ex.Message));
            }
        }


        [HttpGet("chart")]
        public async Task<IActionResult> GetAccountOptionsForProduct(int? productId = null)
        {
            try
            {
                var productAccountOptions = await _chartOfAccountRepository.GetProductAccountOptionsAsync(productId);
                if (productAccountOptions == null)
                {
                    return NotFound(new { message = "No account options found for this user or product." });
                }
                return Ok(new
                {
                    InventoryAssetAccounts = productAccountOptions.InventoryAssetAccounts,
                    IncomeAccounts = productAccountOptions.IncomeAccounts,
                    ExpenseAccounts = productAccountOptions.ExpenseAccounts
                });
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while fetching account options." });
            }
        }


    }
}
