using Application.DTOs;
using Application.DTOs.Medication;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/Medications")]
public class MedicationController : ControllerBase
{
    private readonly IMedicationServices _medicationServices;

    public MedicationController(IMedicationServices medicationServices)
    {
        _medicationServices = medicationServices;
    }

    [HttpGet("search")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        // Reuse pagination logic for search
        var pagination = new PaginationDto { Query = query, Limit = 20, Offset = 0 };
        var result = await _medicationServices.GetAll(pagination);

        if (result.IsSuccess)
        {
            // Return just the items for the search endpoint as expected by frontend
            return Ok(result.Value?.Items);
        }

        return result.Error?.Code switch
        {
            ErrorCodes.BadRequest => BadRequest(result.Error),
            ErrorCodes.NotFound => NotFound(result.Error),
            ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
        };
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
    {
        var result = await _medicationServices.GetAll(pagination);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }
}
