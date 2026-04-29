
using Application.DTOs;
using Application.DTOs.Employee;
using Application.DTOs.ExamType;
using Application.DTOs.Patient;
using Application.Interfaces;
using Application.Services;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/examType")]
public class ExamTypeController : BaseController
{
  private readonly IExamTypeServices _examTypeServices;

  public ExamTypeController(IExamTypeServices examTypeServices)
  {
    _examTypeServices = examTypeServices;
  }
  [HttpGet]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Get([FromQuery] PaginationDto pagination)
  {
    var result = await _examTypeServices.GetAll(pagination);
    return result.IsSuccess ? Ok(new
    {
      count = result.Value?.Count,
      pages = result.Value?.Pages,
      items = result.Value?.Items
    }) : HandleFailure(result);
  }

  [Authorize]
  [HttpPost("createExamType")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> CreateEmployee(ExamTypeCreateDto patient)
  {
    var result = await _examTypeServices.Add(patient);
    return result.IsSuccess ? CreatedAtAction(nameof(GetExamType), new { id = result.Value!.Id }, result.Value) : HandleFailure(result);
  }


  [HttpGet("{id:int}", Name = "GetExamType")]
  [Authorize]
  public async Task<ActionResult<EmployeReponseDto>> GetExamType(int Id)
  {
    var result = await _examTypeServices.GetById(Id);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpPut("{Id:int}")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]

  public async Task<IActionResult> Update([FromBody] ExamTypeUpdateDto dto, int Id)
  {
    var result = await _examTypeServices.Update(dto, Id);
    return result.IsSuccess ? NoContent() : HandleFailure(result);
  }

  // Activate or deactivate an exam type
  [HttpPut("{Id:int}/activate")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Activate(int Id)
  {
    var result = await _examTypeServices.UpdateState(Id);
    return result.IsSuccess ? NoContent() : HandleFailure(result);
  }
}
