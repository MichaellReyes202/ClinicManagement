using Application.DTOs.Prescription;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PrescriptionsController : BaseController
{
  private readonly IPrescriptionServices _prescriptionServices;

  public PrescriptionsController(IPrescriptionServices prescriptionServices)
  {
    _prescriptionServices = prescriptionServices;
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
  {
    var result = await _prescriptionServices.CreatePrescriptionAsync(dto);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("by-consultation/{consultationId}")]
  public async Task<IActionResult> GetByConsultationId(int consultationId)
  {
    var result = await _prescriptionServices.GetByConsultationIdAsync(consultationId);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }

  [HttpGet("by-patient/{patientId}")]
  public async Task<IActionResult> GetByPatientId(int patientId)
  {
    var result = await _prescriptionServices.GetByPatientIdAsync(patientId);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }
}
