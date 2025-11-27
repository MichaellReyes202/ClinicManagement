
using Application.DTOs;
using Application.DTOs.AuditLog;
using Application.DTOs.specialty;
using Application.Interfaces;
using Application.Util;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;


namespace Application.Services;

public class AuditlogServices : IAuditlogServices
{
    private readonly IAuditlogRepository _auditlogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditlogServices(IAuditlogRepository auditlogRepository , IHttpContextAccessor httpContextAccessor)
    {
        _auditlogRepository = auditlogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PaginatedResponseDto<AuditLogListDto>>> GetFilteredLogsAsync(AuditLogQuery queryDto)
    {
        try
        {
            var (query , total) = await _auditlogRepository.GetQueryAndTotal( include: q => q.Include(a => a.Performedbyuser) );

            if (!string.IsNullOrWhiteSpace(queryDto.ModuleName) && Enum.TryParse(queryDto.ModuleName, true, out AuditModuletype moduleType))
            {
                query = query.Where(a => a.Moduleid == (int)moduleType);
            }
            if (!string.IsNullOrWhiteSpace(queryDto.ActionName) && Enum.TryParse(queryDto.ActionName, true, out ActionType actionType))
            {
                query = query.Where(a => a.Actiontype.ToLower() == actionType.ToString().ToLower());
            }
            if (!string.IsNullOrWhiteSpace(queryDto.StatusName) &&  Enum.TryParse(queryDto.StatusName, true, out AuditStatus auditStatus))
            {
                query = query.Where(a => a.Statusid == (int)auditStatus);
            }

            // --- Búsqueda general
            if (!string.IsNullOrWhiteSpace(queryDto.SearchTerm))
            {
                var term = queryDto.SearchTerm.Trim().ToLower();

                query = query.Where(a => a.Recorddisplay.ToLower().Contains(term) ||  (a.Changedetail != null && a.Changedetail.ToLower().Contains(term)) ||
                    (a.Performedbyuser != null && a.Performedbyuser.Email.ToLower().Contains(term))
                );
            }

            // Ordenar
            query = query.OrderByDescending(a => a.Timestamp);

            // Paginación
            var pagination = new  PaginationDto(queryDto.limit, queryDto.offset);

            var rawItems = await query
                .Skip(pagination.Offset)
                .Take(pagination.Limit)
                .AsNoTracking()
                .ToListAsync();

            // --- Construcción del resultado
            var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);

            var result = rawItems.Select(a => new AuditLogListDto
            {
                Id = a.Id,
                UserId = a.Performedbyuserid,
                UserEmail = a.Performedbyuser?.Email ?? "System",
                Module = (AuditModuletype)a.Moduleid,
                ActionType = Enum.Parse<ActionType>(a.Actiontype, true),
                Status = (AuditStatus)a.Statusid,
                RecordId = a.Recordid,
                RecordDisplay = a.Recorddisplay,
                ChangeDetail = a.Changedetail ?? "",
                CreatedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(a.Timestamp, userTimeZone)
            })
            .ToList();
            var paginatedResponse = new PaginatedResponseDto<AuditLogListDto>(total, result , queryDto.limit);
            return Result<PaginatedResponseDto<AuditLogListDto>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            return Result<PaginatedResponseDto<AuditLogListDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }


    public async Task RegisterAuthorizationFailureAsync(int? userId, AuditModuletype module, ActionType actionType, string recordDisplay, AuditStatus status, string changeDetail)
    {
        if (actionType != ActionType.AUTH_DENIED)
        {
            throw new InvalidOperationException("RegisterAuthorizationFailureAsync solo debe usarse para ActionType.AUTH_DENIED.");
        }

        var audit = new Auditlog
        {
            Timestamp = DateTime.UtcNow,
            Performedbyuserid = userId,
            Moduleid = (int)module,
            Actiontype = nameof(ActionType.AUTH_DENIED), 
            Recordid = 0, // No hay registro de negocio directo afectado (cero por defecto)
            Recorddisplay = recordDisplay,
            Statusid = (int)status,
            Changedetail = changeDetail 
        };
        await _auditlogRepository.AddAsync(audit);
        await _auditlogRepository.SaveChangesAsync(); 
    }
    
    public async Task RegisterActionAsync(int? userId, AuditModuletype module, ActionType actionType, string recordDisplay, int recordId, AuditStatus status, string? changeDetail = null)
    {
        var audit = new Auditlog
        {
            Timestamp = DateTime.UtcNow,
            Performedbyuserid = userId,
            Moduleid = (int)module,
            Actiontype = actionType.ToString(), 
            Recordid = recordId,
            Recorddisplay = recordDisplay,
            Statusid = (int)status,
            Changedetail = changeDetail
        };
        await _auditlogRepository.AddAsync(audit);
        await _auditlogRepository.SaveChangesAsync();
    }
}