using Application.DTOs.Appointment;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Validators.Appointment
{
    public class AppointmentCreateDtoValidator : AbstractValidator<AppointmentCreateDto>
    {
        private readonly IGenericRepository<ClinicSchedule> _clinicScheduleRepo;

        public AppointmentCreateDtoValidator(IGenericRepository<ClinicSchedule> clinicScheduleRepo)
        {
            _clinicScheduleRepo = clinicScheduleRepo;

            RuleFor(x => x.PatientId)
                .GreaterThan(0)
                .WithMessage("You must select a patient.");

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0)
                .WithMessage("You must select a doctor.");

            RuleFor(x => x.Duration)
                .InclusiveBetween(5, 150)
                .WithMessage("The duration should be between 5 and 150 minutes.");

            // StartTime: Validaciones básicas
            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("The start date and time are required.")
                // Relaxed check: Allow up to 10 minutes in the past to account for clock drift
                .GreaterThanOrEqualTo(x => DateTime.Now.AddMinutes(-10))
                .WithMessage("Appointments cannot be scheduled in the past.")
                .MustAsync(BeWithinClinicBusinessHoursAsync)
                .WithMessage("The start time must be within the clinic's business hours.");

            // Validación cruzada de duración y hora de finalización
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

        private async Task<bool> BeWithinWorkingDayAsync(AppointmentCreateDto dto, CancellationToken cancellationToken)
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
}