
using Application.DTOs;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployesController  : ControllerBase
    {
        private readonly IEmployesServices _employesServices;

        public EmployesController(IEmployesServices employesServices)
        {
            _employesServices = employesServices;
        }


        // obtener el empleador por el id 
        [HttpGet("{id:int}", Name = "GetEmployee")]
        public async Task<ActionResult<EmployeReponseDto>> Get(int Id)
        {
            var result = await _employesServices.GetEmployeeById(Id);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if (result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result.Error);
            }
            if (result.Error?.Code == ErrorCodes.BadRequest)
            {
                return BadRequest(result.Error);
            }
            return BadRequest(result.Error);

        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] PaginationDto pagination)
        {
            var result = await _employesServices.GetAllEmployes(pagination);
            if (result.IsSuccess)
            {
                return Ok(new
                {
                    count = result.Value?.Count,
                    pages = result.Value?.Pages,
                    EmployeeListDto = result.Value?.Items
                });
            }
            return BadRequest(result.Error);
        }


        [HttpPost("createEmployes")]
        [Authorize]
        [ProducesResponseType( StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateEmployee(EmployesCreationDto employes)
        {
            var result = await _employesServices.AddSpecialtyAsync(employes);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if(result.Error?.Code == ErrorCodes.Conflict)
            {
                return Conflict(result.Error);
            }
            if(result.Error?.Code == ErrorCodes.BadRequest)
            {
                return BadRequest(new { result.Error, result.ValidationErrors });
            }
            if(result.Error?.Code == ErrorCodes.NotFound)
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);

        }

    }
}
