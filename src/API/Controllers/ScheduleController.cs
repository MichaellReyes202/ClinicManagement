using Application.DTOs.Schedule;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/schedules")]
    [Authorize]
    public class ScheduleController : BaseController
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        // ── Horario general de la clínica ──────────────────────────────────

        /// <summary>GET /api/schedules/clinic — Obtiene los 7 días del horario de la clínica.</summary>
        [HttpGet("clinic")]
        [AllowAnonymous] // Necesario para que el frontend de citas lo consulte sin auth
        [ProducesResponseType(typeof(List<ClinicScheduleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClinicSchedules()
        {
            var result = await _scheduleService.GetClinicSchedulesAsync();
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        /// <summary>PUT /api/schedules/clinic/{id} — Actualiza un día del horario de la clínica.</summary>
        [HttpPut("clinic/{id:int}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(ClinicScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClinicSchedule(int id, [FromBody] UpdateClinicScheduleDto dto)
        {
            var result = await _scheduleService.UpdateClinicScheduleAsync(id, dto);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        // ── Horario por empleado/doctor ────────────────────────────────────

        /// <summary>GET /api/schedules/employee/{employeeId} — Obtiene los días configurados para un doctor.</summary>
        [HttpGet("employee/{employeeId:int}")]
        [ProducesResponseType(typeof(List<EmployeeScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeSchedules(int employeeId)
        {
            var result = await _scheduleService.GetEmployeeSchedulesAsync(employeeId);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        /// <summary>PUT /api/schedules/employee/{employeeId}/day — Crea o actualiza el horario de un doctor para un día.</summary>
        [HttpPut("employee/{employeeId:int}/day")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(EmployeeScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertEmployeeSchedule(int employeeId, [FromBody] UpsertEmployeeScheduleDto dto)
        {
            var result = await _scheduleService.UpsertEmployeeScheduleAsync(employeeId, dto);
            return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
        }

        /// <summary>PUT /api/schedules/employee/{employeeId}/duration — Actualiza la duración estándar de cita de un doctor.</summary>
        [HttpPut("employee/{employeeId:int}/duration")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmployeeAppointmentDuration(int employeeId, [FromBody] UpdateEmployeeAppointmentDurationDto dto)
        {
            var result = await _scheduleService.UpdateEmployeeAppointmentDurationAsync(employeeId, dto);
            return result.IsSuccess ? Ok(new { message = "Duración actualizada correctamente." }) : HandleFailure(result);
        }
    }
}
