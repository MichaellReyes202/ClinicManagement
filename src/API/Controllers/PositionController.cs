

using Application.DTOs;
using Application.DTOs.Position;
using Application.DTOs.specialty;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;


[ApiController]
[Route("api/position")]
public class PositionController : BaseController
{
  private readonly IPositionServices _positionServices;

  public PositionController(IPositionServices positionServices)
  {
    _positionServices = positionServices;
  }

  [HttpGet]
  [Authorize]
  public async Task<ActionResult<Result<PaginatedResponseDto<PositionListDto>>>> Get([FromQuery] PaginationDto pagination)
  {
    var result = await _positionServices.GetAllPosition(pagination);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("listOption")]
  public async Task<ActionResult<List<OptionDto>>> GetListOption()
  {
    return await _positionServices.GetAllPositionOptions();
  }

  [HttpPost("create")]
  [Authorize]
  [ProducesResponseType(typeof(Specialty), StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<ActionResult<Specialty>> CreateSpecialty(PositionCreationDto specialtiesDto)
  {
    var result = await _positionServices.AddPositionAsync(specialtiesDto);
    return result.IsSuccess ? CreatedAtAction(nameof(GetPosition), new { id = result.Value!.Id }, result.Value) : HandleFailure(result);
  }

  [HttpGet("{id}")]
  [ProducesResponseType(typeof(Position), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<Position>> GetPosition(int id)
  {
    var result = await _positionServices.GetByIdAsync(id);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpPut("{id:int}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> UpdateSpecialty(int id, PositionUpdateDto specialtiesDto)
  {
    var result = await _positionServices.UpdatePositionAsync(id, specialtiesDto);
    return result.IsSuccess ? NoContent() : HandleFailure(result);
  }
}
