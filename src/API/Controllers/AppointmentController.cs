

using Application.DTOs.Appointment;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/appointment")]
public class AppointmentController : BaseController
{
  private readonly IAppointmentServices _appointmentServices;

  public AppointmentController(IAppointmentServices appointmentServices)
  {
    _appointmentServices = appointmentServices;
  }

  [HttpGet("{id:int}", Name = "GetAppointment")]
  [Authorize]
  public async Task<ActionResult<AppointmentDetailDto>> GetAppointment(int id)
  {
    var result = await _appointmentServices.GetById(id);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }



  [HttpPut("updateStatus")]
  [Authorize]
  public async Task<IActionResult> UpdateStatusAppointments([FromBody] UpdateStatusAppointmenDto dto)
  {
    var result = await _appointmentServices.UpdateStatusAppointmentsAsync(dto);
    return result.IsSuccess ? NoContent() : HandleFailure(result);
  }

  [HttpGet("doctorsAvailability")]
  [Authorize]
  public async Task<ActionResult<List<DoctorAvailabilityDto>>> DoctorAvailabilityAsync([FromQuery] int? specialtyId = null)
  {
    var result = await _appointmentServices.GetDoctorAvailabilityAsync(specialtyId);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("today")]
  [Authorize]
  public async Task<ActionResult<List<TodayAppointmentDto>>> GetTodayAppointments([FromQuery] DateTime? date = null)
  {
    var result = await _appointmentServices.GetTodayAppointmentsAsync(date);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }


  [HttpGet("list")]
  [Authorize]
  public async Task<ActionResult<List<AppointmentListDto>>> GetListAsync([FromQuery] AppointmentFilterDto filter)
  {
    var result = await _appointmentServices.GetListAsync(filter);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }


  [HttpPost("createAppointment")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<int>> Add(AppointmentCreateDto dto)
  {
    var result = await _appointmentServices.Add(dto);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpPut("{Id:int}")]
  [Authorize]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status409Conflict)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> Update([FromBody] AppointmentUpdateDto dto, int Id)
  {
    var result = await _appointmentServices.Update(dto, Id);
    return result.IsSuccess ? NoContent() : HandleFailure(result);

  }
  [HttpDelete("{id:int}")]
  [Authorize]
  public async Task<IActionResult> Delete(int id)
  {
    var result = await _appointmentServices.Delete(id);
    return result.IsSuccess ? NoContent() : HandleFailure(result);
  }
}
