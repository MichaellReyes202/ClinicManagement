

using Application.DTOs;
using Application.DTOs.Employee;
using Application.DTOs.Patient;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : BaseController
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
    return result.IsSuccess ? Ok(new
    {
      count = result.Value?.Count,
      pages = result.Value?.Pages,
      items = result.Value?.Items
    }) : HandleFailure(result);
  }

  [HttpGet("search")]
  [Authorize]
  public async Task<IActionResult> Search([FromQuery] PaginationDto pagination)
  {
    var result = await _patientServices.SearchPatient(pagination);
    return result.IsSuccess ? Ok(new
    {
      count = result.Value?.Count,
      pages = result.Value?.Pages,
      items = result.Value?.Items
    }) : HandleFailure(result);
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
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  // obtener el empleador por el id 
  [HttpGet("{id:int}", Name = "GetPatient")]
  [Authorize]
  public async Task<ActionResult<EmployeReponseDto>> Get(int Id)
  {
    var result = await _patientServices.GetPatientById(Id);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
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
    return result.IsSuccess ? NoContent() : HandleFailure(result);
  }

}