using Application.DTOs;
using Application.DTOs.Medication;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/Medications")]
public class MedicationController : BaseController
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
    var pagination = new PaginationDto { Query = query, Limit = 20, Offset = 0 };
    var result = await _medicationServices.GetAll(pagination);
    return result.IsSuccess ? Ok(result.Value?.Items) : HandleFailure(result);
  }

  [HttpGet]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAll([FromQuery] PaginationDto pagination)
  {
    var result = await _medicationServices.GetAll(pagination);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }
}
