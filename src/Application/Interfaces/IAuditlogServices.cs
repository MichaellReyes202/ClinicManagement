
using Application.DTOs;
using Application.DTOs.AuditLog;
using Domain.Enums;
using Domain.Errors;

namespace Application.Interfaces;

public interface IAuditlogServices 
{
    Task RegisterAuthorizationFailureAsync(  int? userId, AuditModuletype module,ActionType actionType,  string recordDisplay,  AuditStatus status, string changeDetail);
    Task RegisterActionAsync( int? userId,  AuditModuletype module, ActionType actionType,  string recordDisplay, int recordId, AuditStatus status,  string? changeDetail = null );
    Task<Result<PaginatedResponseDto<AuditLogListDto>>> GetFilteredLogsAsync(AuditLogQuery queryDto);
}