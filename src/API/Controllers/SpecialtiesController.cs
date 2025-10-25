
using Application.DTOs;
using Application.DTOs.specialty;
using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/specialties")]
public class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtiesServices _specialtiesServices;

    public SpecialtiesController(ISpecialtiesServices specialtiesServices)
    {
        _specialtiesServices = specialtiesServices;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<Result<PaginatedResponseDto<SpecialtyListDto>>>> Get([FromQuery] PaginationDto pagination)
    {
        var result = await _specialtiesServices.GetAllSpecialties(pagination);
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
    [HttpGet("listOption")]
    public async Task<ActionResult<List<OptionDto>>> GetListOption()
    {
        return await _specialtiesServices.GetAllSpecialtiesOptions();
    }

    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(Specialty), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Specialty>> CreateSpecialty(SpecialtiesCreateDto specialtiesDto)
    {
        var result = await _specialtiesServices.AddSpecialtyAsync(specialtiesDto);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetSpecialty), new { id = result.Value!.Id }, result.Value);
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

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Specialty), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Specialty>> GetSpecialty(int id)
    {
        var result = await _specialtiesServices.GetByIdAsync(id);
        if (result.IsSuccess)
            return Ok(result.Value);
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

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSpecialty(int id, SpecialtiesUpdateDto specialtiesDto)
    {
        var result = await _specialtiesServices.UpdateSpecialtyAsync(id, specialtiesDto);
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
