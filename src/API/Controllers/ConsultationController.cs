using Application.DTOs.Consultation;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ConsultationController : BaseController
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
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpPost("finalize")]
  public async Task<IActionResult> FinalizeConsultation([FromBody] FinishConsultationDto dto)
  {
    var result = await _consultationServices.FinishConsultationAsync(dto);
    return result.IsSuccess ? Ok(new { message = "Consultation finalized successfully" }) : HandleFailure(result);
  }

  [HttpPost("rollback/{id}")]
  public async Task<IActionResult> RollbackConsultation(int id)
  {
    var result = await _consultationServices.RollbackConsultationAsync(id);
    return result.IsSuccess ? Ok(new { message = "Consultation rolled back successfully" }) : HandleFailure(result);

  }

  [HttpGet("by-appointment/{appointmentId}")]
  public async Task<IActionResult> GetByAppointmentId(int appointmentId)
  {
    var result = await _consultationServices.GetConsultationByAppointmentIdAsync(appointmentId);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("by-patient/{patientId}")]
  public async Task<IActionResult> GetByPatient(int patientId)
  {
    var result = await _consultationServices.GetConsultationsByPatientIdAsync(patientId);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("all")]
  public async Task<IActionResult> GetAll()
  {
    var result = await _consultationServices.GetAllConsultationsAsync();
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("{id}/pdf")]
  public async Task<IActionResult> GetConsultationPdf(int id)
  {
    // Nota: El parámetro «id» se trata aquí como AppointmentId para que coincida con los datos disponibles en la interfaz de usuario (appt.id).
    // Si quisieramos estrictamente ConsultationId, la interfaz de usuario tendría que recuperarlo primero o necesitaríamos una ruta diferente.
    // Reutilizar GetConsultationByAppointmentIdAsync es la opción más eficiente dado el contexto actual.

    var result = await _consultationServices.GetConsultationByAppointmentIdAsync(id);

    if (!result.IsSuccess)
    {
      return NotFound(result.Error);
    }
    var pdfBytes = _pdfService.GenerateConsultationReport(result.Value!);
    return File(pdfBytes, "application/pdf", $"consulta_{id}.pdf");
  }
}
