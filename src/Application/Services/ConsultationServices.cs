using Application.DTOs.Consultation;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class ConsultationServices : IConsultationServices
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUserService _userService;
    private readonly IAuditlogServices _auditlogServices;

    public ConsultationServices(
        IConsultationRepository consultationRepository,
        IAppointmentRepository appointmentRepository,
        IUserService userService,
        IAuditlogServices auditlogServices)
    {
        _consultationRepository = consultationRepository;
        _appointmentRepository = appointmentRepository;
        _userService = userService;
        _auditlogServices = auditlogServices;
    }

    public async Task<Result<int>> StartConsultationAsync(int appointmentId)
    {
        using var transaction = await _consultationRepository.BeginTransactionAsync();
        try
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

            await transaction.CommitAsync();

            return Result<int>.Success(consultation.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<int>.Failure(new Error(ErrorCodes.Unexpected, $"Error starting consultation: {ex.Message}"));
        }
    }

    public async Task<Result> FinishConsultationAsync(FinishConsultationDto dto)
    {
        using var transaction = await _consultationRepository.BeginTransactionAsync();
        try
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

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"Error finishing consultation: {ex.Message}"));
        }
    }

    public async Task<Result> RollbackConsultationAsync(int consultationId)
    {
        using var transaction = await _consultationRepository.BeginTransactionAsync();
        try
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

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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
                    MedicationName = pi.Medication.Name,
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
                    MedicationName = pi.Medication.Name,
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
                    MedicationName = pi.Medication.Name,
                    Dose = pi.Dose,
                    Frequency = pi.Frequency,
                    Duration = pi.Duration
                }).ToList()
            }).ToList()
        }).ToList();

        return Result<List<ConsultationDetailDto>>.Success(dtos);
    }
}
