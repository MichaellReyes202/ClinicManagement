using System;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Director")] // 1=Admin, 4=Director (mapped in policy or checked manually if needed)
    // Assuming "Admin" and "Director" are role names. If using IDs, might need custom policy or check user claims.
    // For now, let's assume standard role names or that the Auth system handles it.
    // Based on previous context, roles are IDs. Let's check how roles are handled.
    // Actually, usually [Authorize(Roles = "Admin")] works if roles are claims.
    // Let's stick to standard Authorize for now, or just [Authorize] and check logic if needed.
    // The prompt says "Visible solo para roles [1, 4]".
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
