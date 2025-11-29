
using Application.DTOs.Appointment;
using FluentValidation;

namespace Application.Validators.Appointment;
public class AppointmentUpdateDtoValidator : AbstractValidator<AppointmentUpdateDto>
{

    private static readonly DateTime Now = DateTime.Now;
    private static readonly DateTime TwoHoursFromNow = Now.AddHours(2);
    public AppointmentUpdateDtoValidator()
    {
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
            .GreaterThanOrEqualTo(Now)
            .WithMessage("Appointments cannot be scheduled in the past.")
            .Must(BeWithinBusinessHours)
            .WithMessage("The start time must be within the permitted hours (Mon-Fri 8:00-17:00, Sat 8:00-12:00).");

        // Validación cruzada: cita termina >= 2.5 horas después de ahora + dentro del horario
        RuleFor(x => x)
            .Must(x =>
            {
                var endTime = x.StartTime.AddMinutes(x.Duration);
                return endTime >= TwoHoursFromNow;
            })
            .WithMessage("The appointment must end at least 2 hours after the current time.")
            .OverridePropertyName("StartTime");

        RuleFor(x => x)
            .Must(x =>
            {
                var endTime = x.StartTime.AddMinutes(x.Duration);
                return BeWithinBusinessHours(endTime);
            })
            .WithMessage("The end time must be within the allowed time.")
            .OverridePropertyName("Duration");
    }

    private static bool BeWithinBusinessHours(DateTime dateTime)
    {
        var day = dateTime.DayOfWeek;
        var time = dateTime.TimeOfDay;

        if (day == DayOfWeek.Sunday)
            return false;

        if (day == DayOfWeek.Saturday)
            return time >= TimeSpan.FromHours(8) && time <= TimeSpan.FromHours(17);

        // Lunes a viernes
        return time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(17.5);
    }
}
