
using Application.DTOs;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.DTOs.Employee;

namespace API.Controllers
{
  [ApiController]
  [Route("api/employees")]
  public class EmployesController : BaseController
  {
    private readonly IEmployesServices _employesServices;

    public EmployesController(IEmployesServices employesServices)
    {
      _employesServices = employesServices;
    }

    [HttpGet("search")]
    [Authorize]
    public async Task<IActionResult> SearchEmployees([FromQuery] PaginationDto pagination)
    {
      var result = await _employesServices.EmployeesWithoutUsers(pagination);
      return result.IsSuccess ? Ok(new
      {
        count = result.Value?.Count,
        pages = result.Value?.Pages,
        EmployeeListSearchDto = result.Value?.Items
      }) : HandleFailure(result);
    }
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] PaginationDto pagination)
    {
      var result = await _employesServices.GetAllEmployes(pagination);
      return result.IsSuccess ? Ok(new
      {
        count = result.Value?.Count,
        pages = result.Value?.Pages,
        EmployeeListDto = result.Value?.Items
      }) : HandleFailure(result);
    }


    // obtener el empleador por el id 
    [HttpGet("{id:int}", Name = "GetEmployee")]
    public async Task<ActionResult<EmployeReponseDto>> Get(int Id)
    {
      var result = await _employesServices.GetEmployeeById(Id);
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }


    [Authorize]
    [HttpPost("createEmployes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEmployee(EmployesCreationDto employes)
    {
      var result = await _employesServices.AddEmployesAsync(employes);
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);

    }

    [HttpPut("{Id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> UpdateEmployee([FromBody] EmployesUpdateDto dto, int Id)
    {
      var result = await _employesServices.UpdateEmployesAsync(dto, Id);
      return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

  }
}
