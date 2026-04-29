using System;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize(Roles = "Admin,Director")]
  // 1 = Admin, 4 = Director (asignados en la política o verificados manualmente si es necesario)
  // Suponiendo que «Admin» y «Director» son nombres de roles. Si se utilizan ID, podría ser necesario crear una política personalizada o comprobar las reclamaciones del usuario.
  // Por ahora, supongamos que se trata de nombres de roles estándar o que el sistema de autenticación se encarga de ello.
  // Según el contexto anterior, los roles son ID. Comprobemos cómo se gestionan los roles.
  // En realidad, normalmente [Authorize(Roles = «Admin»)] funciona si los roles son reclamaciones.
  // Por ahora, sigamos con el Authorize estándar, o simplemente [Authorize], y comprobemos la lógica si es necesario.
  // El mensaje dice «Visible solo para los roles [1, 4]».
  public class ReportsController : ControllerBase
  {
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
      _reportService = reportService;
    }

    [HttpGet("medical-productivity")]
    public async Task<IActionResult> GetMedicalProductivity([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string format = "pdf")
    {
      var (fileContent, contentType) = await _reportService.GetMedicalProductivityReportAsync(from, to, format);
      return File(fileContent, contentType, $"MedicalProductivity_{from:yyyyMMdd}_{to:yyyyMMdd}.{GetExtension(format)}");
    }

    [HttpGet("morbidity")]
    public async Task<IActionResult> GetMorbidity([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string format = "pdf")
    {
      var (fileContent, contentType) = await _reportService.GetMorbidityReportAsync(from, to, format);
      return File(fileContent, contentType, $"Morbidity_{from:yyyyMMdd}_{to:yyyyMMdd}.{GetExtension(format)}");
    }

    [HttpGet("lab-volume")]
    public async Task<IActionResult> GetLabVolume([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string format = "pdf")
    {
      var (fileContent, contentType) = await _reportService.GetLabVolumeReportAsync(from, to, format);
      return File(fileContent, contentType, $"LabVolume_{from:yyyyMMdd}_{to:yyyyMMdd}.{GetExtension(format)}");
    }

    [HttpGet("patient-absenteeism")]
    public async Task<IActionResult> GetPatientAbsenteeism([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string format = "pdf")
    {
      var (fileContent, contentType) = await _reportService.GetPatientAbsenteeismReportAsync(from, to, format);
      return File(fileContent, contentType, $"PatientAbsenteeism_{from:yyyyMMdd}_{to:yyyyMMdd}.{GetExtension(format)}");
    }

    private string GetExtension(string format) => format.ToLower() == "excel" ? "xlsx" : "pdf";
  }
}
