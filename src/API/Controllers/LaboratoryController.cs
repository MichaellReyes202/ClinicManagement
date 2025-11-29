using Application.DTOs.Laboratory;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LaboratoryController : ControllerBase
{
    private readonly ILaboratoryServices _laboratoryServices;

    public LaboratoryController(ILaboratoryServices laboratoryServices)
    {
        _laboratoryServices = laboratoryServices;
    }

    [HttpPost("order")]
    public async Task<IActionResult> CreateOrder([FromBody] ExamOrderDto dto)
    {
        var result = await _laboratoryServices.CreateExamOrderAsync(dto);
        if (result.IsSuccess)
        {
            return Ok(new { message = "Exam order created successfully" });
        }
        return BadRequest(result.Error);
    }

    [HttpPut("process")]
    public async Task<IActionResult> ProcessExam([FromBody] ExamProcessDto dto)
    {
        var result = await _laboratoryServices.ProcessExamAsync(dto);
        if (result.IsSuccess)
        {
            return Ok(new { message = "Exam processed successfully" });
        }
        return BadRequest(result.Error);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingExams()
    {
        var result = await _laboratoryServices.GetPendingExamsAsync();
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }

    [HttpGet("by-appointment/{appointmentId}")]
    public async Task<IActionResult> GetByAppointmentId(int appointmentId)
    {
        var result = await _laboratoryServices.GetExamsByAppointmentIdAsync(appointmentId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }
    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var result = await _laboratoryServices.GetExamsByPatientIdAsync(patientId);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllExams()
    {
        var result = await _laboratoryServices.GetAllExamsAsync();
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(result.Error);
    }
}
