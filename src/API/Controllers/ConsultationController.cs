using Application.DTOs.Consultation;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ConsultationController : ControllerBase
{
    private readonly IConsultationServices _consultationServices;

    public ConsultationController(IConsultationServices consultationServices)
    {
        _consultationServices = consultationServices;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartConsultation([FromBody] StartConsultationDto dto)
    {
        var result = await _consultationServices.StartConsultationAsync(dto.AppointmentId);
        if (result.IsSuccess)
        {
            return Ok(new { consultationId = result.Value });
        }
        return BadRequest(result.Error);
    }

    [HttpPost("finalize")]
    public async Task<IActionResult> FinalizeConsultation([FromBody] FinishConsultationDto dto)
    {
        var result = await _consultationServices.FinishConsultationAsync(dto);
        if (result.IsSuccess)
        {
            return Ok(new { message = "Consultation finalized successfully" });
        }
        return BadRequest(result.Error);
    }

    [HttpPost("rollback/{id}")]
    public async Task<IActionResult> RollbackConsultation(int id)
    {
        var result = await _consultationServices.RollbackConsultationAsync(id);
        if (result.IsSuccess)
        {
            return Ok(new { message = "Consultation rolled back successfully" });
        }
        return BadRequest(result.Error);
    }

    [HttpGet("by-appointment/{appointmentId}")]
    public async Task<IActionResult> GetByAppointmentId(int appointmentId)
    {
        var result = await _consultationServices.GetConsultationByAppointmentIdAsync(appointmentId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return NotFound(result.Error);
    }

    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var result = await _consultationServices.GetConsultationsByPatientIdAsync(patientId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
