using Application.DTOs.Consultation;
using Application.DTOs.Appointment;
using Application.Interfaces;
using Application.Util;
using Application.Validators.Appointment;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class ConsultationServices : IConsultationServices
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IEmployesRepository _employesRepository;
    private readonly AppointmentCreateDtoValidator _appointmentCreateValidator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private readonly IAuditlogServices _auditlogServices;

    public ConsultationServices(
        IConsultationRepository consultationRepository,
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IEmployesRepository employesRepository,
        AppointmentCreateDtoValidator appointmentCreateValidator,
        IHttpContextAccessor httpContextAccessor,
        IUserService userService,
        IAuditlogServices auditlogServices)
    {
        _consultationRepository = consultationRepository;
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _employesRepository = employesRepository;
        _appointmentCreateValidator = appointmentCreateValidator;
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
        _auditlogServices = auditlogServices;
    }

    public async Task<Result<StartWalkInConsultationResponseDto>> StartWalkInConsultationAsync(StartWalkInConsultationDto dto)
    {
        var timeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);
        var nowUtc = DateTime.UtcNow;
        var localStart = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone),
            DateTimeKind.Unspecified);
        const int durationMinutes = 30;

        var appointmentDto = new AppointmentCreateDto
        {
            PatientId = dto.PatientId,
            EmployeeId = dto.EmployeeId,
            StartTime = localStart,
            Duration = durationMinutes,
            Reason = "Consulta sin cita previa (Walk-in)"
        };

        var validation = await _appointmentCreateValidator.ValidateAsync(appointmentDto);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(error => new ValidationError(error.PropertyName, error.ErrorMessage))
                .ToList();
            return Result<StartWalkInConsultationResponseDto>.Failure(errors);
        }

        var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var endTimeUtc = startTimeUtc.AddMinutes(durationMinutes);

        try
        {
            return await _consultationRepository.ExecuteInTransactionAsync(async () =>
            {
                var patientExists = await _patientRepository.ExistAsync(patient => patient.Id == dto.PatientId);
                if (!patientExists)
                {
                    return Result<StartWalkInConsultationResponseDto>.Failure(
                        new Error(ErrorCodes.NotFound, "El paciente no existe o está inactivo.", "PatientId"));
                }

                var doctorExists = await _employesRepository.ExistAsync(employee =>
                    employee.Id == dto.EmployeeId && employee.IsActive == true && employee.PositionId == 1);
                if (!doctorExists)
                {
                    return Result<StartWalkInConsultationResponseDto>.Failure(
                        new Error(ErrorCodes.NotFound, "El médico no existe o está inactivo.", "EmployeeId"));
                }

                var patientHasOverlap = await _appointmentRepository.ExistAsync(appointment =>
                    appointment.PatientId == dto.PatientId &&
                    appointment.StartTime < endTimeUtc &&
                    (appointment.EndTime ?? appointment.StartTime.AddMinutes(appointment.Duration)) > startTimeUtc &&
                    appointment.StatusId != (int)AppointmentStatus.Cancelled &&
                    appointment.StatusId != (int)AppointmentStatus.Expired);
                if (patientHasOverlap)
                {
                    return Result<StartWalkInConsultationResponseDto>.Failure(
                        new Error(ErrorCodes.Conflict, "El paciente ya tiene una cita activa o programada en este horario.", "StartTime"));
                }

                var doctorHasOverlap = await _appointmentRepository.ExistAsync(appointment =>
                    appointment.EmployeeId == dto.EmployeeId &&
                    appointment.StartTime < endTimeUtc &&
                    (appointment.EndTime ?? appointment.StartTime.AddMinutes(appointment.Duration)) > startTimeUtc &&
                    appointment.StatusId != (int)AppointmentStatus.Cancelled &&
                    appointment.StatusId != (int)AppointmentStatus.Expired);
                if (doctorHasOverlap)
                {
                    return Result<StartWalkInConsultationResponseDto>.Failure(
                        new Error(ErrorCodes.Conflict, "El médico ya tiene una cita en este horario.", "StartTime"));
                }

                var currentUser = await _userService.GetCurrentUserAsync();
                var now = DateTime.UtcNow;
                var appointment = new Appointment
                {
                    PatientId = dto.PatientId,
                    EmployeeId = dto.EmployeeId,
                    StartTime = startTimeUtc,
                    EndTime = endTimeUtc,
                    Duration = durationMinutes,
                    Reason = appointmentDto.Reason,
                    StatusId = (int)AppointmentStatus.InProgress,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = currentUser?.Id
                };

                await _appointmentRepository.AddAsync(appointment);
                await _appointmentRepository.SaveChangesAsync();

                var consultation = new Consultation
                {
                    AppointmentId = appointment.Id,
                    PatientId = dto.PatientId,
                    EmployeeId = dto.EmployeeId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = currentUser?.Id,
                    IsFinalized = false
                };

                await _consultationRepository.AddAsync(consultation);
                await _consultationRepository.SaveChangesAsync();

                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: AuditModuletype.Appointments,
                    actionType: ActionType.CREATE,
                    recordDisplay: $"Consulta sin cita previa para Paciente #{dto.PatientId}",
                    recordId: appointment.Id,
                    status: AuditStatus.SUCCESS);

                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: AuditModuletype.Appointments,
                    actionType: ActionType.STATUS_CHANGE,
                    recordDisplay: $"Consulta iniciada para Cita #{appointment.Id}",
                    recordId: consultation.Id,
                    status: AuditStatus.SUCCESS);

                return Result<StartWalkInConsultationResponseDto>.Success(new StartWalkInConsultationResponseDto
                {
                    AppointmentId = appointment.Id,
                    ConsultationId = consultation.Id
                });
            });
        }
        catch (Exception ex)
        {
            return Result<StartWalkInConsultationResponseDto>.Failure(
                new Error(ErrorCodes.Unexpected, $"Error al iniciar la consulta: {ex.Message}"));
        }
    }

    public async Task<Result<int>> StartConsultationAsync(int appointmentId)
    {
        try
        {
            return await _consultationRepository.ExecuteInTransactionAsync(async () =>
            {
                var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
                if (appointment == null)
                {
                    return Result<int>.Failure(new Error(ErrorCodes.NotFound, "Appointment not found"));
                }

                if (appointment.StatusId != 2) // Must be Confirmed
                {
                    return Result<int>.Failure(new Error(ErrorCodes.BadRequest, "Appointment is not in 'Confirmed' status"));
                }

                // Check if consultation already exists
                var existingConsultation = await _consultationRepository.GetQuery(c => c.AppointmentId == appointmentId);
                if (await existingConsultation.AnyAsync())
                {
                    return Result<int>.Failure(new Error(ErrorCodes.Conflict, "Consultation already exists for this appointment"));
                }

                var currentUser = await _userService.GetCurrentUserAsync();

                var consultation = new Consultation
                {
                    AppointmentId = appointmentId,
                    PatientId = appointment.PatientId ?? 0, // Should verify nullability
                    EmployeeId = appointment.EmployeeId ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = currentUser?.Id,
                    IsFinalized = false
                };

                await _consultationRepository.AddAsync(consultation);
                await _consultationRepository.SaveChangesAsync();

                // Update Appointment Status to 3 (En Curso)
                appointment.StatusId = 3;
                await _appointmentRepository.UpdateAsync(appointment);
                await _appointmentRepository.SaveChangesAsync();

                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: AuditModuletype.Appointments,
                    actionType: ActionType.STATUS_CHANGE,
                    recordDisplay: $"Consulta iniciada para Cita #{appointmentId}",
                    recordId: consultation.Id,
                    status: AuditStatus.SUCCESS
                );

                return Result<int>.Success(consultation.Id);
            });
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(new Error(ErrorCodes.Unexpected, $"Error starting consultation: {ex.Message}"));
        }
    }

    public async Task<Result> FinishConsultationAsync(FinishConsultationDto dto)
    {
        try
        {
            return await _consultationRepository.ExecuteInTransactionAsync(async () =>
            {
                var consultation = await _consultationRepository.GetByIdAsync(dto.ConsultationId);
                if (consultation == null)
                {
                    return Result.Failure(new Error(ErrorCodes.NotFound, "Consultation not found"));
                }

                if (consultation.IsFinalized == true)
                {
                    return Result.Failure(new Error(ErrorCodes.BadRequest, "Consultation is already finalized"));
                }

                var currentUser = await _userService.GetCurrentUserAsync();

                consultation.Reason = dto.Reason;
                consultation.PhysicalExam = dto.PhysicalExam;
                consultation.Diagnosis = dto.Diagnosis;
                consultation.TreatmentNotes = dto.TreatmentNotes;
                consultation.IsFinalized = true;
                consultation.FinalizedAt = DateTime.UtcNow;
                consultation.UpdatedAt = DateTime.UtcNow;
                consultation.UpdatedByUserId = currentUser?.Id;

                await _consultationRepository.UpdateAsync(consultation);
                await _consultationRepository.SaveChangesAsync();

                // Update Appointment Status to 4 (Completed)
                if (consultation.AppointmentId.HasValue)
                {
                    var appointment = await _appointmentRepository.GetByIdAsync(consultation.AppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.StatusId = 4;
                        await _appointmentRepository.UpdateAsync(appointment);
                        await _appointmentRepository.SaveChangesAsync();
                    }
                }

                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: AuditModuletype.Appointments,
                    actionType: ActionType.STATUS_CHANGE,
                    recordDisplay: $"Consulta finalizada #{consultation.Id}",
                    recordId: consultation.Id,
                    status: AuditStatus.SUCCESS
                );

                return Result.Success();
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"Error finishing consultation: {ex.Message}"));
        }
    }

    public async Task<Result> RollbackConsultationAsync(int consultationId)
    {
        try
        {
            return await _consultationRepository.ExecuteInTransactionAsync(async () =>
            {
                var consultation = await _consultationRepository.GetByIdAsync(consultationId);
                if (consultation == null)
                {
                    return Result.Failure(new Error(ErrorCodes.NotFound, "Consultation not found"));
                }

                if (consultation.IsFinalized == true)
                {
                    return Result.Failure(new Error(ErrorCodes.BadRequest, "Cannot rollback a finalized consultation"));
                }

                // Revert Appointment Status to 2 (Confirmed)
                if (consultation.AppointmentId.HasValue)
                {
                    var appointment = await _appointmentRepository.GetByIdAsync(consultation.AppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.StatusId = 2;
                        await _appointmentRepository.UpdateAsync(appointment);
                        await _appointmentRepository.SaveChangesAsync();
                    }
                }

                // Delete Consultation
                await _consultationRepository.DeleteAsync(consultation);
                await _consultationRepository.SaveChangesAsync();

                return Result.Success();
            });
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"Error rolling back consultation: {ex.Message}"));
        }
    }

    public async Task<Result<ConsultationDetailDto>> GetConsultationByAppointmentIdAsync(int appointmentId)
    {
        var query = await _consultationRepository.GetQuery(c => c.AppointmentId == appointmentId);
        var consultation = await query
            .Include(c => c.Patient)
            .Include(c => c.Employee)
            .Include(c => c.Exams).ThenInclude(e => e.ExamType)
            .Include(c => c.Exams).ThenInclude(e => e.Status)
            .Include(c => c.Prescriptions).ThenInclude(p => p.PrescriptionItems).ThenInclude(pi => pi.Medication)
            .FirstOrDefaultAsync<Consultation>();

        if (consultation == null)
        {
            return Result<ConsultationDetailDto>.Failure(new Error(ErrorCodes.NotFound, "Consultation not found for this appointment"));
        }

        var dto = new ConsultationDetailDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId ?? 0,
            PatientId = consultation.PatientId,
            PatientName = $"{consultation.Patient.FirstName} {consultation.Patient.LastName}",
            DoctorId = consultation.EmployeeId,
            DoctorName = $"{consultation.Employee.FirstName} {consultation.Employee.LastName}",
            Reason = consultation.Reason,
            PhysicalExam = consultation.PhysicalExam,
            Diagnosis = consultation.Diagnosis,
            TreatmentNotes = consultation.TreatmentNotes,
            IsFinalized = consultation.IsFinalized ?? false,
            FinalizedAt = consultation.FinalizedAt,
            CreatedAt = consultation.CreatedAt,
            Exams = consultation.Exams.Select(e => new ConsultationExamDto
            {
                Id = e.Id,
                ExamTypeName = e.ExamType.Name,
                Status = e.Status.Name,
                Results = e.Results
            }).ToList(),
            Prescriptions = consultation.Prescriptions.Select(p => new ConsultationPrescriptionDto
            {
                Id = p.Id,
                Notes = p.Notes,
                Items = p.PrescriptionItems.Select(pi => new ConsultationPrescriptionItemDto
                {
                    MedicationName = pi.Medication?.Name ?? string.Empty,
                    Dose = pi.Dose,
                    Frequency = pi.Frequency,
                    Duration = pi.Duration
                }).ToList()
            }).ToList()
        };

        return Result<ConsultationDetailDto>.Success(dto);
    }
        
    
    public async Task<Result<List<ConsultationDetailDto>>> GetConsultationsByPatientIdAsync(int patientId)
    {
        var query = await _consultationRepository.GetQuery(c => c.PatientId == patientId);
        var consultations = await query
            .Include(c => c.Patient)
            .Include(c => c.Employee)
            .Include(c => c.Exams).ThenInclude(e => e.ExamType)
            .Include(c => c.Exams).ThenInclude(e => e.Status)
            .Include(c => c.Prescriptions).ThenInclude(p => p.PrescriptionItems).ThenInclude(pi => pi.Medication)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var dtos = consultations.Select(consultation => new ConsultationDetailDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId ?? 0,
            PatientId = consultation.PatientId,
            PatientName = $"{consultation.Patient.FirstName} {consultation.Patient.LastName}",
            DoctorId = consultation.EmployeeId,
            DoctorName = $"{consultation.Employee.FirstName} {consultation.Employee.LastName}",
            Reason = consultation.Reason,
            PhysicalExam = consultation.PhysicalExam,
            Diagnosis = consultation.Diagnosis,
            TreatmentNotes = consultation.TreatmentNotes,
            IsFinalized = consultation.IsFinalized ?? false,
            FinalizedAt = consultation.FinalizedAt,
            CreatedAt = consultation.CreatedAt,
            Exams = consultation.Exams.Select(e => new ConsultationExamDto
            {
                Id = e.Id,
                ExamTypeName = e.ExamType.Name,
                Status = e.Status.Name,
                Results = e.Results
            }).ToList(),
            Prescriptions = consultation.Prescriptions.Select(p => new ConsultationPrescriptionDto
            {
                Id = p.Id,
                Notes = p.Notes,
                Items = p.PrescriptionItems.Select(pi => new ConsultationPrescriptionItemDto
                {
                    MedicationName = pi.Medication?.Name ?? string.Empty,
                    Dose = pi.Dose,
                    Frequency = pi.Frequency,
                    Duration = pi.Duration
                }).ToList()
            }).ToList()
        }).ToList();

        return Result<List<ConsultationDetailDto>>.Success(dtos);
    }

    public async Task<Result<List<ConsultationDetailDto>>> GetAllConsultationsAsync()
    {
        var query = await _consultationRepository.GetQuery();
        var consultations = await query
            .Include(c => c.Patient)
            .Include(c => c.Employee)
            .Include(c => c.Exams).ThenInclude(e => e.ExamType)
            .Include(c => c.Exams).ThenInclude(e => e.Status)
            .Include(c => c.Prescriptions).ThenInclude(p => p.PrescriptionItems).ThenInclude(pi => pi.Medication)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var dtos = consultations.Select(consultation => new ConsultationDetailDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId ?? 0,
            PatientId = consultation.PatientId,
            PatientName = $"{consultation.Patient.FirstName} {consultation.Patient.LastName}",
            DoctorId = consultation.EmployeeId,
            DoctorName = $"{consultation.Employee.FirstName} {consultation.Employee.LastName}",
            Reason = consultation.Reason,
            PhysicalExam = consultation.PhysicalExam,
            Diagnosis = consultation.Diagnosis,
            TreatmentNotes = consultation.TreatmentNotes,
            IsFinalized = consultation.IsFinalized ?? false,
            FinalizedAt = consultation.FinalizedAt,
            CreatedAt = consultation.CreatedAt,
            Exams = consultation.Exams.Select(e => new ConsultationExamDto
            {
                Id = e.Id,
                ExamTypeName = e.ExamType.Name,
                Status = e.Status.Name,
                Results = e.Results
            }).ToList(),
            Prescriptions = consultation.Prescriptions.Select(p => new ConsultationPrescriptionDto
            {
                Id = p.Id,
                Notes = p.Notes,
                Items = p.PrescriptionItems.Select(pi => new ConsultationPrescriptionItemDto
                {
                    MedicationName = pi.Medication?.Name ?? string.Empty,
                    Dose = pi.Dose,
                    Frequency = pi.Frequency,
                    Duration = pi.Duration
                }).ToList()
            }).ToList()
        }).ToList();

        return Result<List<ConsultationDetailDto>>.Success(dtos);
    }
}
