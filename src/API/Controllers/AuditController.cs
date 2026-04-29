

using Application.DTOs;
using Application.DTOs.AuditLog;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = "RequireAdmin")] // Solo administradores deben acceder a logs
public class AuditController : BaseController
{
  private readonly IAuditlogServices _auditlogServices;

  public AuditController(IAuditlogServices auditlogServices)
  {
    _auditlogServices = auditlogServices;
  }

  [HttpGet]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public async Task<ActionResult<PaginatedResponseDto<AuditLogListDto>>> GetAuditLogs([FromQuery] AuditLogQuery query)
  {
    var result = await _auditlogServices.GetFilteredLogsAsync(query);
    return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
  }
}