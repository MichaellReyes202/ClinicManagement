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
    private readonly IPdfService _pdfService;

    public ConsultationController(IConsultationServices consultationServices, IPdfService pdfService)
    {
        _consultationServices = consultationServices;
        _pdfService = pdfService;
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

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _consultationServices.GetAllConsultationsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetConsultationPdf(int id)
    {
        // Note: The 'id' parameter here is treated as AppointmentId to match the frontend's available data (appt.id).
        // If we strictly wanted ConsultationId, the frontend would need to fetch it first or we'd need a different route.
        // Reusing GetConsultationByAppointmentIdAsync is the most efficient path given current context.
        
        var result = await _consultationServices.GetConsultationByAppointmentIdAsync(id);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        var pdfBytes = _pdfService.GenerateConsultationReport(result.Value);

        return File(pdfBytes, "application/pdf", $"consulta_{id}.pdf");
    }
}
