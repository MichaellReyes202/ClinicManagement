

using Application.DTOs;
using Application.DTOs.AuditLog;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = "RequireAdmin")] // Solo administradores deben acceder a logs
public class AuditController : ControllerBase
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
        if (result.IsSuccess)
        {
            return result.Value!;
        }
        if (result.ValidationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = "One or more validation errors have occurred in the provided fields.",
                errors = result.ValidationErrors
            });
        }
        return result.Error?.Code switch
        {
            ErrorCodes.BadRequest => BadRequest(result.Error),
            ErrorCodes.Conflict => Conflict(result.Error),
            ErrorCodes.NotFound => NotFound(result.Error),
            ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
        };
    }
}