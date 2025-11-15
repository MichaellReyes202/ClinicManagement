

using Application.DTOs;
using Application.DTOs.Employee;
using Application.DTOs.Patient;
using Application.Interfaces;
using Application.Services;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly IPatientServices _patientServices;

    public PatientsController(IPatientServices patientServices)
    {
        _patientServices = patientServices;
    }
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] PaginationDto pagination)
    {
        var result = await _patientServices.GetAllPatients(pagination);
        if (result.IsSuccess)
        {
            return Ok(new
            {
                count = result.Value?.Count,
                pages = result.Value?.Pages,
                items = result.Value?.Items
            });
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

    [HttpGet("search")]
    [Authorize]
    public async Task<IActionResult> Search([FromQuery] PaginationDto pagination)
    {
        var result = await _patientServices.SearchPatient(pagination);
        if (result.IsSuccess)
        {
            return Ok(new
            {
                count = result.Value?.Count,
                pages = result.Value?.Pages,
                items = result.Value?.Items
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

    [Authorize]
    [HttpPost("createPatient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEmployee(PatientCreateDto patient)
    {
        var result = await _patientServices.AddPatientAsync(patient);

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

    // obtener el empleador por el id 
    [HttpGet("{id:int}", Name = "GetPatient")]
    [Authorize]
    public async Task<ActionResult<EmployeReponseDto>> Get(int Id)
    {
        var result = await _patientServices.GetPatientById(Id);
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

    [HttpPut("{Id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEmployee([FromBody] PatientUpdateDto dto, int Id)
    {
        var result = await _patientServices.UpdatePatientAsync(dto, Id);
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