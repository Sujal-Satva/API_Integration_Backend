
using Microsoft.AspNetCore.Mvc;
using task_14.Services;
using Microsoft.EntityFrameworkCore;
using DataAccess.Services;
using SharedModels.Models;
using task_14.Data;

namespace task_14.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthRepository _authRepository;
        private readonly ApplicationDbContext _context;
        private readonly IConnectionRepository _connectionRepository;
        public AuthController(IConfiguration configuration,ApplicationDbContext context,IAuthRepository authRepository,IConnectionRepository connectionRepository)
        {
            _configuration = configuration;
            _authRepository = authRepository;
            _connectionRepository = connectionRepository;
            _context = context;
        }


        [HttpGet("qbo-login")]
        public IActionResult GetRedirectURL()
        {
            var clientId = _configuration["QuickBooks:ClientId"];
            var redirectUri = _configuration["QuickBooks:RedirectUri"];
            var scope = "com.intuit.quickbooks.accounting";
            var state = Guid.NewGuid().ToString();
            var authUrl = _configuration["QuickBooks:AuthUrl"];
            var url = authUrl +
                      $"?client_id={clientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope={Uri.EscapeDataString(scope)}" +
                      $"&state={state}";

            return Redirect(url);
        }

        [HttpGet("xero-login")]
        public IActionResult Authorize()
        {
            var clientId = _configuration["Xero:ClientId"];
            var redirectUri = _configuration["Xero:RedirectUri"];
            var scope = _configuration["Xero:Scope"];
            var authUrl = _configuration["Xero:AuthUrl"];
            var authorizationUrl = $"{authUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scope)}";
            return Redirect(authorizationUrl);
        }


        [HttpGet("qbo-callback")]
        public async Task<IActionResult> QuickBooksCallback([FromQuery] string code, [FromQuery] string state, [FromQuery] string realmId)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new CommonResponse<object>(400, "Authorization code not found."));
            }

            var result = await _authRepository.HandleQuickBooksCallbackAsync(code, realmId, state);

            if (result == null)
            {
                return StatusCode(500, new CommonResponse<object>(500, "Unexpected error occurred."));
            }

            if (result.Status != 200)
            {
                return StatusCode(result.Status, new CommonResponse<object>(result.Status, result.Message,result.Data));
            }

            return Ok(new CommonResponse<object>(200, result.Message, new
            {
                realmId = result.Data.RealmId,
                accessToken = result.Data.AccessToken,
                refreshToken = result.Data.RefreshToken
            }));
        }


        [HttpGet("xero-callback")]
        public async Task<IActionResult> XeroCallback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new CommonResponse<object>(400, "Authorization code is required."));
            }

            var result = await _authRepository.HandleXeroCallbackAsync(code);

            if (result == null)
            {
                return StatusCode(500, new CommonResponse<object>(500, "Unexpected error occurred."));
            }

            if (result!= null )
            {
                return StatusCode(result.Status, new CommonResponse<object>(result.Status, result.Message, result.Data));
            }

            return Ok(new CommonResponse<object>(200, result.Message, new
            {
                tenantId = result.Data.TenantId,
                tenantName = result.Data.TenantName
            }));
        }


        [HttpGet("connection-status")]
        public async Task<IActionResult> GetConnectionStatus()
        {
            var quickbooksConnection = await _context.Connections
                .Where(c => c.SourceAccounting == "QuickBooks")
                .Select(c => new { c.ExternalId })
                .FirstOrDefaultAsync();

            var xeroConnection = await _context.Connections
                .Where(c => c.SourceAccounting == "Xero")
                .Select(c => new { c.ExternalId })
                .FirstOrDefaultAsync();

            var response = new CommonResponse<object>(200, "Connection status fetched successfully", new
            {
                quickbooksConnected = quickbooksConnection != null,
                quickbooksConnectionId = quickbooksConnection?.ExternalId,
                xeroConnected = xeroConnection != null,
                xeroConnectionId = xeroConnection?.ExternalId
            });

            return Ok(response);
        }


        [HttpGet("disconnect-connection")]
        public async Task<IActionResult> DeleteConnection([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new CommonResponse<string>(404, "Id is missing", null));
            }
            var result = await _connectionRepository.DeleteConnectionAsync(id);

            if (result == null)
            {
                return StatusCode(500, new CommonResponse<object>(500, "Unexpected error occurred."));
            }

            if (result != null)
            {
                return StatusCode(result.Status, new CommonResponse<object>(result.Status, result.Message));
            }
            return StatusCode(result.Status, new CommonResponse<object>(result.Status, result.Message));
        }
    }
}
