using Application.DTOs.Appointment;
using Application.DTOs.Patient;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services;
public class AppointmentServices : IAppointmentServices
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IValidator<AppointmentCreateDto> _createValidator;
    private readonly IValidator<AppointmentUpdateDto> _updateValidator;
    private readonly IPatientRepository _patientRepository;
    private readonly IEmployesRepository _employesRepository;
    private readonly ICatalogServices _catalogServices;
    private readonly IUserService _userService;
    private readonly IMapper _mapper;

    public AppointmentServices(
        IAppointmentRepository appointmentRepository , 
        IValidator<AppointmentCreateDto> createValidator ,
        IValidator<AppointmentUpdateDto> updateValidator,
        IPatientRepository patientRepository ,
        IEmployesRepository employesRepository,
        ICatalogServices catalogServices,
        IUserService userService,
        IMapper mapper
    )
    {
        _appointmentRepository = appointmentRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _patientRepository = patientRepository;
        _employesRepository = employesRepository;
        _catalogServices = catalogServices;
        _userService = userService;
        _mapper = mapper;
    }


    public async Task<Result<List<DoctorAvailabilityDto>>> GetDoctorAvailabilityAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        // Calcular slots totales según el día
        static int GetTotalSlots(DateTime date)
        {
            return date.DayOfWeek switch { DayOfWeek.Sunday => 0, DayOfWeek.Saturday => 8,   // 8:00 - 12:00 = 4h = 8 slots
                _ => 18                    // L-V: 8:00 - 17:00 = 9h = 18 slots
            };
        }

        int totalSlotsToday = GetTotalSlots(today);
        if (totalSlotsToday == 0)
        {
            return Result<List<DoctorAvailabilityDto>>.Success(new List<DoctorAvailabilityDto>());
        }
        var query = await _employesRepository.GetQuery(filter : e => e.IsActive == true && e.PositionId == 1); // Solo doctores


        var result = await query
            .Select(e => new DoctorAvailabilityDto
            {
                DoctorId = e.Id,
                FullName = $"{e.FirstName} {(string.IsNullOrEmpty(e.MiddleName) ? "" : e.MiddleName + " ")}{e.LastName} {(string.IsNullOrEmpty(e.SecondLastName) ? "" : e.SecondLastName)}".Trim(),
                SpecialtyName = e.Specialty != null ? e.Specialty.Name : "General",

                // Disponible si NO está en cita "En curso"
                IsAvailable = !e.Appointments.Any(a =>
                    a.StatusId == 3 &&
                    a.StartTime <= now &&
                    (a.EndTime == null || a.EndTime >= now)
                ),

                // Próxima cita futura
                NextAppointmentTime = e.Appointments
                    .Where(a => a.StartTime > now)
                    .OrderBy(a => a.StartTime)
                    .Select(a => (DateTime?)a.StartTime)
                    .FirstOrDefault(),

                // Citas de hoy (solo dentro del horario)
                AppointmentsTodayCount = e.Appointments
                    .Count(a =>
                        a.StartTime >= today &&
                        a.StartTime < tomorrow &&
                        a.StartTime.TimeOfDay >= TimeSpan.FromHours(8) &&
                        a.StartTime.TimeOfDay < (today.DayOfWeek == DayOfWeek.Saturday ? TimeSpan.FromHours(12) : TimeSpan.FromHours(17))
                    ),

                AvailableSlotsToday = 0 // se calcula después
            })
            .ToListAsync();

        // Post-procesamiento
        foreach (var doc in result)
        {
            int slotsUsed = doc.AppointmentsTodayCount;
            doc.AvailableSlotsToday = Math.Max(0, totalSlotsToday - slotsUsed);

            // Formato próximo horario
            if (doc.NextAppointmentTime.HasValue)
            {
                var next = doc.NextAppointmentTime.Value;
                var isTomorrow = next.Date == tomorrow;
                var timeStr = next.ToString("hh:mm tt", new System.Globalization.CultureInfo("es-ES")).ToUpper();
                doc.NextAppointmentDisplay = isTomorrow ? $"Mañana {timeStr}" : timeStr;
            }
            else
            {
                var nextDay = today;
                while (GetTotalSlots(nextDay) == 0)
                    nextDay = nextDay.AddDays(1);

                var startHour = nextDay.DayOfWeek == DayOfWeek.Saturday ? 8 : 8;
                doc.NextAppointmentTime = nextDay.AddHours(startHour);
                doc.NextAppointmentDisplay = nextDay.Date == tomorrow ? "Mañana 08:00 AM"  : nextDay.ToString("ddd hh:mm tt", new System.Globalization.CultureInfo("es-ES")).ToUpper();
            }
        }

        var items =  result.OrderBy(d => d.FullName).ToList();
        return Result<List<DoctorAvailabilityDto>>.Success(items);
    }

    public async Task<Result<List<AppointmentListDto>>> GetListAsync()
    {
        var query = await _appointmentRepository.GetQuery(
            include: q => q.Include(p => p.Employee).Include(q => q.Employee).Include(p => p.Status)
        );

        var items =  await query
        .Select(a => new AppointmentListDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientFullName = a.Patient.FirstName + " " + a.Patient.LastName,
            EmployeeId = a.EmployeeId,
            DoctorSpecialtyId = a.Employee.SpecialtyId,
            StartTime = a.StartTime,
            DoctorFullName = a.Employee.FirstName + " " + a.Employee.LastName,
            EndTime = (DateTime)a.EndTime,
            Duration = a.Duration,
            Reason = a.Reason,
            Status   = a.Status.Name ,
            StatusId = a.Status.Id,
        })
        .OrderBy(a => a.StartTime)
        .ToListAsync();

        return Result<List<AppointmentListDto>>.Success(items);
    }
    public async Task<Result<AppointmentResponseDto>> Add(AppointmentCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<AppointmentResponseDto>.Failure(errors);
        }

        using var transaction = await _appointmentRepository.BeginTransactionAsync();
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync();
            var patientExists = await _patientRepository.ExistAsync(p => p.Id == dto.PatientId);
            if (!patientExists)
            {
                return Result<AppointmentResponseDto>.Failure( new Error(ErrorCodes.NotFound, "The patient does not exist or is inactive", "PatientId"));
            }
            // se valida que el empleado sea uno que tenga el cargo de doctor
            var doctorExists = await _employesRepository.ExistAsync(e => e.Id == dto.EmployeeId && e.IsActive.HasValue && e.PositionId == 1);
            if (!doctorExists)
            {
                return Result<AppointmentResponseDto>.Failure( new Error(ErrorCodes.NotFound, "The doctor does not exist or is inactive.", "EmployeeId"));
            }

            // FORZAR UTC DESDE EL INICIO
            var startTimeUtc = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
            var endTimeUtc = startTimeUtc.AddMinutes(dto.Duration);
            var appointmentDate = DateOnly.FromDateTime(startTimeUtc);

            // Paciente: máximo 1 cita por día
            var patientHasAppointmentToday = await _appointmentRepository.ExistAsync(a => a.PatientId == dto.PatientId && DateOnly.FromDateTime(a.StartTime) == appointmentDate);

            if (patientHasAppointmentToday)
            {
                return Result<AppointmentResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "The patient already has an appointment scheduled for this day.", "StartTime"));
            }

            // Médico: no superposición
            var doctorHasOverlap = await _appointmentRepository.ExistAsync(a => a.EmployeeId == dto.EmployeeId && a.StartTime < endTimeUtc && a.StartTime.AddMinutes(a.Duration) > startTimeUtc);
            if (doctorHasOverlap)
            {
                return Result<AppointmentResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "The doctor already has an appointment at that time.", "StartTime"));
            }

            // Mapear y guardar en UTC
            var appointment = _mapper.Map<Appointment>(dto);
            appointment.StartTime = startTimeUtc;
            appointment.EndTime = startTimeUtc.AddMinutes(dto.Duration); 
            appointment.StatusId = (int)AppointmentStatus.Scheduled;
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.CreatedByUserId = currentUser?.Id;

            await _appointmentRepository.AddAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = _mapper.Map<AppointmentResponseDto>(appointment);
            return Result<AppointmentResponseDto>.Success(response);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Result<AppointmentResponseDto>.Failure( new Error(ErrorCodes.Unexpected, "Error saving to database."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result<AppointmentResponseDto>.Failure(  new Error(ErrorCodes.Unexpected, "Unexpected error creating appointment."));
        }
    }

    public async Task<Result> Update(AppointmentUpdateDto dto, int patientId)
    {
        var valitationResult = await _updateValidator.ValidateAsync(dto);
        if (!valitationResult.IsValid)
        {
            var errors = valitationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result.Failure(errors);
        }
        using var transaction = await _patientRepository.BeginTransactionAsync();
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync();

            // verificar que la cita exista

            var appointment = await _appointmentRepository.GetByIdAsync(dto.Id);
            if(appointment is null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The appointment does not exist", "PatientId"));
            }
            var patientExists = await _patientRepository.ExistAsync(p => p.Id == dto.PatientId);
            if (!patientExists)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The patient does not exist or is inactive", "PatientId"));
            }
            // se valida que el empleado sea uno que tenga el cargo de doctor
            var doctorExists = await _employesRepository.ExistAsync(e => e.Id == dto.EmployeeId && e.IsActive.HasValue && e.PositionId == 1);
            if (!doctorExists)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The doctor does not exist or is inactive.", "EmployeeId"));
            }
            // verificar el codigo para el estado de la cita exista
            var existStatusId = await _catalogServices.ExistAppointmentStatus(dto.StatusId);
            if (!existStatusId)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, $"the code for the appointment status {dto.StatusId} was not found", "StatusId"));
            }

            // FORZAR UTC DESDE EL INICIO
            var startTimeUtc = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
            var endTimeUtc = startTimeUtc.AddMinutes(dto.Duration);
            var appointmentDate = DateOnly.FromDateTime(startTimeUtc);      

            // Médico: no superposición
            var doctorHasOverlap = await _appointmentRepository
                .ExistAsync(a => a.EmployeeId == dto.EmployeeId && a.StartTime < endTimeUtc && a.StartTime.AddMinutes(a.Duration) > startTimeUtc && a.Id != dto.Id);
            
            if (doctorHasOverlap)
            {
                return Result.Failure(new Error(ErrorCodes.BadRequest, "The doctor already has an appointment at that time.", "StartTime"));
            }
            // Mapear y guardar en UTC
            _mapper.Map(dto,appointment);
            appointment.StartTime = startTimeUtc;
            appointment.EndTime = startTimeUtc.AddMinutes(dto.Duration);
            appointment.StatusId = dto.StatusId;
            appointment.UpdatedAt = DateTime.UtcNow;
            appointment.UpdatedByUserId = currentUser?.Id;
            await _appointmentRepository.UpdateAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, "Error saving to database."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, "Unexpected error creating appointment."));
        }

    }
}
