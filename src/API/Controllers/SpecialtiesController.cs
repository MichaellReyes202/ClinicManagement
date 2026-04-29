
using Application.DTOs;
using Application.DTOs.ExamType;
using Application.DTOs.specialty;
using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/specialties")]
public class SpecialtiesController : BaseController
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
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }


  [HttpGet("examsBySpecialty")]
  [Authorize]
  public async Task<ActionResult<Result<PaginatedResponseDto<SpecialtyListDto>>>> ExamsBySpecialty([FromQuery] PaginationDto pagination)
  {
    var result = await _specialtiesServices.GetExamsBySpecialty(pagination);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);


  }


  [HttpGet("doctorsBySpecialty")]
  [Authorize]
  public async Task<ActionResult<List<DoctorBySpecialtyDto>>> DoctorsBySpecialty([FromQuery] PaginationDto pagination)
  {
    var result = await _specialtiesServices.GetDoctorBySpecialty(pagination);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
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
    return result.IsSuccess ? CreatedAtAction(nameof(GetSpecialty), new { id = result.Value!.Id }, result.Value) : HandleFailure(result);
  }

  [HttpGet("{id}")]
  [ProducesResponseType(typeof(Specialty), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<Specialty>> GetSpecialty(int id)
  {
    var result = await _specialtiesServices.GetByIdAsync(id);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpPut("{id:int}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> UpdateSpecialty(int id, SpecialtiesUpdateDto specialtiesDto)
  {
    var result = await _specialtiesServices.UpdateSpecialtyAsync(id, specialtiesDto);
    return result.IsSuccess ? NoContent() : HandleFailure(result);

  }
}
