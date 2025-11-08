using Application.DTOs.Appointment;
using FluentValidation;
using System;

namespace Application.Validators.Appointment
{
    public class AppointmentCreateDtoValidator : AbstractValidator<AppointmentCreateDto>
    {
        public AppointmentCreateDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .GreaterThan(0)
                .WithMessage("You must select a patient.");

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0)
                .WithMessage("You must select a doctor.");

            RuleFor(x => x.Duration)
                .InclusiveBetween(5, 150)
                .WithMessage("The duration must be between 5 and 150 minutes.");

            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("Start time is required.")
                .Must(BeTomorrowOrLater)
                .WithMessage("Appointments can only be scheduled starting from tomorrow.")
                .Must(BeWithinBusinessHours)
                .WithMessage("The appointment start time must be within business hours (Mon-Fri 8:00 AM to 5:00 PM; Sat 8:00 AM to 12:00 PM).");

            RuleFor(x => x)
                .Must(x => IsEndTimeWithinBusinessHours(x.StartTime, x.Duration))
                .WithMessage("The appointment end time must also be within business hours.");
        }
        private static bool BeTomorrowOrLater(DateTime startTime)
        {
            var tomorrow = DateTime.Today.AddDays(1); // Mañana a las 00:00
            return startTime.Date >= tomorrow.Date;
        }

        private static bool BeWithinBusinessHours(DateTime dateTime)
        {
            var day = dateTime.DayOfWeek;
            var time = dateTime.TimeOfDay;

            if (day == DayOfWeek.Sunday)
                return false;

            if (day == DayOfWeek.Saturday)
                return time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(12);

            // Lunes a viernes
            return time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(17);
        }
        private static bool IsEndTimeWithinBusinessHours(DateTime startTime, int durationMinutes)
        {
            if (durationMinutes <= 0) return false;
            var endTime = startTime.AddMinutes(durationMinutes);
            return BeWithinBusinessHours(endTime);
        }
    }
}