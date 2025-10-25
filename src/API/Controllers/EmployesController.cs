
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
    public class EmployesController  : ControllerBase
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
            if(result.IsSuccess)
            {
                return Ok(new
                {
                    count = result.Value?.Count,
                    pages = result.Value?.Pages,
                    EmployeeListSearchDto = result.Value?.Items
                });
            }
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
        }
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
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
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
        }


        [Authorize]
        [HttpPost("createEmployes")]
        [ProducesResponseType( StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateEmployee(EmployesCreationDto employes)
        {
            var result = await _employesServices.AddEmployesAsync(employes);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if (result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    message = "One or more validation errors have occurred in the provided fields.",
                    errors = result.ValidationErrors
                });
            }
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };

        }

        [HttpPut("{Id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> UpdateEmployee([FromBody] EmployesUpdateDto dto , int Id)
        {
            var result = await _employesServices.UpdateEmployesAsync(dto, Id);
            if (result.IsSuccess)
            {
                return NoContent();
            }
            if (result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    message = "One or more validation errors have occurred in the provided fields.",
                    errors = result.ValidationErrors
                });
            }
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
        }

    }
}
