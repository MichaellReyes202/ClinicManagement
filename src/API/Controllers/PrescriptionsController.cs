using Application.DTOs.Prescription;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PrescriptionsController : ControllerBase
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
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }

    [HttpGet("by-consultation/{consultationId}")]
    public async Task<IActionResult> GetByConsultationId(int consultationId)
    {
        var result = await _prescriptionServices.GetByConsultationIdAsync(consultationId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return NotFound(result.Error);
    }

    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatientId(int patientId)
    {
        var result = await _prescriptionServices.GetByPatientIdAsync(patientId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }
}
