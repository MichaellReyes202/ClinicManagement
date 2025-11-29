using Application.DTOs.Laboratory;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class LaboratoryServices : ILaboratoryServices
{
    private readonly IExamRepository _examRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUserService _userService;
    private readonly IAuditlogServices _auditlogServices;
    private readonly IEmployesRepository _employesRepository;

    public LaboratoryServices(
        IExamRepository examRepository,
        IAppointmentRepository appointmentRepository,
        IUserService userService,
        IAuditlogServices auditlogServices,
        IEmployesRepository employesRepository)
    {
        _examRepository = examRepository;
        _appointmentRepository = appointmentRepository;
        _userService = userService;
        _auditlogServices = auditlogServices;
        _employesRepository = employesRepository;
    }

    public async Task<Result> CreateExamOrderAsync(ExamOrderDto dto)
    {
        using var transaction = await _examRepository.BeginTransactionAsync();
        try
        {
            var appointment = await _appointmentRepository.GetByIdAsync(dto.AppointmentId);
            if (appointment == null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "Appointment not found"));
            }

            var currentUser = await _userService.GetCurrentUserAsync();

            foreach (var examTypeId in dto.ExamTypeIds)
            {
                var exam = new Exam
                {
                    AppointmentId = dto.AppointmentId,
                    ConsultationId = dto.ConsultationId, // Can be null
                    ExamTypeId = examTypeId,
                    StatusId = 1, // Programado
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _examRepository.AddAsync(exam);
            }

            await _examRepository.SaveChangesAsync();

            await _auditlogServices.RegisterActionAsync(
                userId: currentUser?.Id,
                module: AuditModuletype.System, // Or a new module for Lab
                actionType: ActionType.CREATE,
                recordDisplay: $"Orden de Exámenes para Cita #{dto.AppointmentId}",
                recordId: dto.AppointmentId,
                status: AuditStatus.SUCCESS
            );

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"Error creating exam order: {ex.Message}"));
        }
    }

    public async Task<Result> ProcessExamAsync(ExamProcessDto dto)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(dto.ExamId);
            if (exam == null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "Exam not found"));
            }

            if (exam.StatusId == 2) // Realizado
            {
                return Result.Failure(new Error(ErrorCodes.BadRequest, "Exam is already processed"));
            }

            var currentUser = await _userService.GetCurrentUserAsync();
            // Find employee associated with current user
            // Assuming 1-to-1 relation or logic to find employee
            // For now, we might need to inject EmployesRepository to find the employee ID of the current user
            var employeeQuery = await _employesRepository.GetQuery(e => e.UserId == currentUser.Id);
            var employee = await employeeQuery.FirstOrDefaultAsync();
            
            exam.Results = dto.Results;
            exam.StatusId = 2; // Realizado
            exam.PerformedByEmployeeId = employee?.Id;
            exam.UpdatedAt = DateTime.UtcNow;

            await _examRepository.UpdateAsync(exam);
            await _examRepository.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"Error processing exam: {ex.Message}"));
        }
    }

    public async Task<Result<List<ExamPendingDto>>> GetPendingExamsAsync()
    {
        var query = await _examRepository.GetQuery(e => e.StatusId == 1 || e.StatusId == 3); // Programado or En Proceso
        var exams = await query
            .Include(e => e.ExamType)
            .Include(e => e.Appointment).ThenInclude(a => a.Patient)
            .Include(e => e.Status)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var dtos = exams.Select(e => new ExamPendingDto
        {
            Id = e.Id,
            ExamTypeId = e.ExamTypeId,
            ExamTypeName = e.ExamType.Name,
            PatientId = e.Appointment?.PatientId ?? 0,
            PatientName = e.Appointment?.Patient != null ? $"{e.Appointment.Patient.FirstName} {e.Appointment.Patient.LastName}" : "Unknown",
            StatusId = e.StatusId,
            StatusName = e.Status.Name,
            Results = e.Results,
            CreatedAt = e.CreatedAt
        }).ToList();

        return Result<List<ExamPendingDto>>.Success(dtos);
    }

    public async Task<Result<List<ExamPendingDto>>> GetExamsByAppointmentIdAsync(int appointmentId)
    {
        var query = await _examRepository.GetQuery(e => e.AppointmentId == appointmentId);
        var exams = await query
            .Include(e => e.ExamType)
            .Include(e => e.Appointment).ThenInclude(a => a.Patient)
            .Include(e => e.Status)
            .ToListAsync();

        var dtos = exams.Select(e => new ExamPendingDto
        {
            Id = e.Id,
            ExamTypeId = e.ExamTypeId,
            ExamTypeName = e.ExamType.Name,
            PatientId = e.Appointment?.PatientId ?? 0,
            PatientName = e.Appointment?.Patient != null ? $"{e.Appointment.Patient.FirstName} {e.Appointment.Patient.LastName}" : "Unknown",
            StatusId = e.StatusId,
            StatusName = e.Status.Name,
            Results = e.Results,
            CreatedAt = e.CreatedAt
        }).ToList();

        return Result<List<ExamPendingDto>>.Success(dtos);
    }
    public async Task<Result<List<ExamPendingDto>>> GetExamsByPatientIdAsync(int patientId)
    {
        var query = await _examRepository.GetQuery(e => e.Appointment.PatientId == patientId);
        var exams = await query
            .Include(e => e.ExamType)
            .Include(e => e.Appointment).ThenInclude(a => a.Patient)
            .Include(e => e.Status)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var dtos = exams.Select(e => new ExamPendingDto
        {
            Id = e.Id,
            ExamTypeId = e.ExamTypeId,
            ExamTypeName = e.ExamType.Name,
            PatientId = e.Appointment?.PatientId ?? 0,
            PatientName = e.Appointment?.Patient != null ? $"{e.Appointment.Patient.FirstName} {e.Appointment.Patient.LastName}" : "Unknown",
            StatusId = e.StatusId,
            StatusName = e.Status.Name,
            Results = e.Results,
            CreatedAt = e.CreatedAt
        }).ToList();

        return Result<List<ExamPendingDto>>.Success(dtos);
    }
}
