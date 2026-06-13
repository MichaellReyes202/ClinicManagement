
using Application.DTOs.Appointment;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Validators.Appointment;
public class AppointmentUpdateDtoValidator : AbstractValidator<AppointmentUpdateDto>
{
    private readonly IGenericRepository<ClinicSchedule> _clinicScheduleRepo;

    public AppointmentUpdateDtoValidator(IGenericRepository<ClinicSchedule> clinicScheduleRepo)
    {
        _clinicScheduleRepo = clinicScheduleRepo;

        // id de la cita
        RuleFor( x => x.Id ).NotNull().GreaterThan(0).WithMessage("appointment id is required");
        // Status de la cita
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("You must select the status of the appointment");

        RuleFor(x => x.PatientId) .GreaterThan(0) .WithMessage("You must select a patient.");
        RuleFor(x => x.EmployeeId).GreaterThan(0) .WithMessage("You must select a doctor.");
        RuleFor(x => x.Duration).InclusiveBetween(5, 150).WithMessage("The duration should be between 5 and 150 minutes.");

        // StartTime: no vacío, no en el pasado, dentro de horario
        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("The start date and time are required.")
            .GreaterThanOrEqualTo(x => DateTime.Now.AddMinutes(-10))
            .WithMessage("Appointments cannot be scheduled in the past.")
            .MustAsync(BeWithinClinicBusinessHoursAsync)
            .WithMessage("The start time must be within the clinic's business hours.");

        // Validación cruzada: cita termina >= 2 horas después de ahora
        RuleFor(x => x)
            .Must(x =>
            {
                var endTime = x.StartTime.AddMinutes(x.Duration);
                return endTime >= DateTime.Now.AddHours(2);
            })
            .WithMessage("The appointment must end at least 2 hours after the current time.")
            .OverridePropertyName("StartTime");

        RuleFor(x => x)
            .MustAsync(BeWithinWorkingDayAsync)
            .WithMessage("The appointment must start and end within the clinic's business hours on the same day.")
            .OverridePropertyName("Duration");
    }

    private async Task<bool> BeWithinClinicBusinessHoursAsync(DateTime startTime, CancellationToken cancellationToken)
    {
        var dayOfWeek = (short)startTime.DayOfWeek;
        var query = await _clinicScheduleRepo.GetQuery(filter: s => s.DayOfWeek == dayOfWeek);
        var schedule = await query.FirstOrDefaultAsync(cancellationToken);

        if (schedule == null || !schedule.IsOpen)
            return false;

        var time = TimeOnly.FromDateTime(startTime);
        return time >= schedule.OpenTime && time < schedule.CloseTime;
    }

    private async Task<bool> BeWithinWorkingDayAsync(AppointmentUpdateDto dto, CancellationToken cancellationToken)
    {
        var startTime = dto.StartTime;
        var endTime = startTime.AddMinutes(dto.Duration);

        var startDayOfWeek = (short)startTime.DayOfWeek;
        var endDayOfWeek = (short)endTime.DayOfWeek;

        // No permitir cruzar días (debe finalizar el mismo día)
        if (startDayOfWeek != endDayOfWeek)
            return false;

        var query = await _clinicScheduleRepo.GetQuery(filter: s => s.DayOfWeek == startDayOfWeek);
        var schedule = await query.FirstOrDefaultAsync(cancellationToken);

        if (schedule == null || !schedule.IsOpen)
            return false;

        var startTimeOnly = TimeOnly.FromDateTime(startTime);
        var endTimeOnly = TimeOnly.FromDateTime(endTime);

        return startTimeOnly >= schedule.OpenTime && endTimeOnly <= schedule.CloseTime;
    }
}
