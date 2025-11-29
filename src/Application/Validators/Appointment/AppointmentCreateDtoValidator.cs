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
                .WithMessage("The duration should be between 5 and 150 minutes.");

            // StartTime: Validaciones básicas
            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("The start date and time are required.")
                // Relaxed check: Allow up to 10 minutes in the past to account for clock drift
                .GreaterThanOrEqualTo(x => DateTime.Now.AddMinutes(-10))
                .WithMessage("Appointments cannot be scheduled in the past.");
                //.Must(BeWithinBusinessHours)
                //.WithMessage("The start time must be within the permitted hours (Mon-Fri 08:00-17:00, Sat 08:00-12:00).");

            /*
            // Validación cruzada: Que la hora de FIN caiga dentro del horario laboral
            RuleFor(x => x)
                .Must(x =>
                {
                    var endTime = x.StartTime.AddMinutes(x.Duration);
                    return BeWithinBusinessHours(endTime);
                })
                .WithMessage("The appointment duration extends beyond working hours.")
                .OverridePropertyName("Duration");
            */
        }

        private bool BeWithinBusinessHours(DateTime dateTime)
        {
            var day = dateTime.DayOfWeek;
            var time = dateTime.TimeOfDay;

            if (day == DayOfWeek.Sunday)
                return false;

            if (day == DayOfWeek.Saturday)
                // Sábados 8:00 AM - 5:00 PM
                return time >= TimeSpan.FromHours(8) && time <= TimeSpan.FromHours(17);

            // Lunes a Viernes 8:00 AM - 5:00 PM
            return time >= TimeSpan.FromHours(8) && time <= TimeSpan.FromHours(17);
        }
    }
}