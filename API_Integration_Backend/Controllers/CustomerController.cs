using Microsoft.AspNetCore.Mvc;
using task_14.Models;
using SharedModels.Models;
using task_14.Services;
using task_14.Repository;
using SharedModels.QuickBooks.Models;


[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITokenRespository _tokenRespository;

    public CustomerController(ICustomerRepository customerRepository, ITokenRespository tokRespository)
    {
        _customerRepository = customerRepository;
        _tokenRespository = tokRespository;
    }

    [HttpGet("fetch-qbo")]
    public async Task<IActionResult> FetchItemsFromQuickBooks()
    {
        try
        {
            var response = await _customerRepository.SyncCustomers("QuickBooks");

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
            var response = await _customerRepository.SyncCustomers("Xero");
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
    public async Task<IActionResult> GetCustomersFromDb(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortColumn = "Name",
            [FromQuery] string? sortDirection = "asc",
            [FromQuery] bool pagination = true,
            [FromQuery] string? sourceSystem = null,
            [FromQuery] bool active = true)
    {
        try
        {
            var response = await _customerRepository.GetCustomersFromDbAsync(
                page, pageSize, search, sortColumn, sortDirection, sourceSystem, active, pagination);

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

    [HttpPost("add")]
    public async Task<IActionResult> AddCustomer([FromBody] CustomerInputModel inputModel, [FromQuery] string platform)
    {
        var response = await _customerRepository.AddCustomersAsync(platform, inputModel) ;
        return StatusCode(response.Status, response);
    }

    [HttpPut("edit")]
    public async Task<IActionResult> EditCustomer([FromBody] CustomerInputModel inputModel, [FromQuery] string id,[FromQuery] string platform)
    {
        var response = await _customerRepository.EditCustomersAsync(platform,id,inputModel);
        return StatusCode(response.Status, response);
    }


    //[HttpPut("update-status")]
    //public async Task<IActionResult> UpdateStatus([FromQuery] string id, [FromQuery] string platform, [FromBody] UpdateStatusRequest request) {
    //    var response = await _customerRepository.UpdateStatus(id, platform, request.Status);
    //    return StatusCode(response.Status, response);

    //}
    //public class UpdateStatusRequest
    //{
    //    public bool Status { get; set; }
    //}

}
