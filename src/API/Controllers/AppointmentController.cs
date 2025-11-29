

using Application.DTOs.Appointment;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/appointment")]
public class AppointmentController : ControllerBase
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
        if (result.IsSuccess)
        {
            return result.Value!;
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



    [HttpPut("updateStatus")]
    [Authorize]
    public async Task<IActionResult> UpdateStatusAppointments([FromBody] UpdateStatusAppointmenDto dto)
    {
        var result = await _appointmentServices.UpdateStatusAppointmentsAsync(dto);

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

    [HttpGet("doctorsAvailability")]
    [Authorize]
    public async Task<ActionResult<List<DoctorAvailabilityDto>>> DoctorAvailabilityAsync([FromQuery] int? specialtyId = null )
    {
        var result = await _appointmentServices.GetDoctorAvailabilityAsync(specialtyId);
        if (result.IsSuccess)
        {
            return result.Value!;
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

    [HttpGet("today")]
    [Authorize]
    public async Task<ActionResult<List<TodayAppointmentDto>>> GetTodayAppointments([FromQuery] DateTime? date = null)
    {
        var result = await _appointmentServices.GetTodayAppointmentsAsync(date);

        if (result.IsSuccess)
        {
            return result.Value!;
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


    [HttpGet("list")]
    [Authorize]
    public async Task<ActionResult<List<AppointmentListDto>>> GetListAsync([FromQuery] AppointmentFilterDto filter)
    {
        var result = await _appointmentServices.GetListAsync(filter);
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


    [HttpPost("createAppointment")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<int>> Add(AppointmentCreateDto dto)
    {
        var result = await _appointmentServices.Add(dto);
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
    public async Task<IActionResult> Update([FromBody] AppointmentUpdateDto dto, int Id)
    {
        var result = await _appointmentServices.Update(dto, Id);
        Console.WriteLine(dto.StatusId); // 

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
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _appointmentServices.Delete(id);
        if (result.IsSuccess)
        {
            return NoContent();
        }
        return result.Error?.Code switch
        {
            ErrorCodes.NotFound => NotFound(result.Error),
            ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
            _ => BadRequest(result.Error)
        };
    }
}
