

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
                // CORRECCIÓN 1: Usar DateTime.Now dinámico (con una pequeña tolerancia de 1 min por latencia)
                .GreaterThanOrEqualTo(DateTime.Now.AddMinutes(-1))
                .WithMessage("Appointments cannot be scheduled in the past.")
                .Must(BeWithinBusinessHours)
                .WithMessage("The start time must be within the permitted hours (Mon-Fri 08:00-17:00, Sat 08:00-12:00).");

            // Validación cruzada: Antelación mínima
            // "La cita debe agendarse con al menos 2 horas de anticipación al momento actual"
            RuleFor(x => x)
                .Must(x =>
                {
                    // CORRECCIÓN 2: Calcular 'Now' en el momento de la validación
                    var minBookingTime = DateTime.Now.AddHours(2);
                    return x.StartTime >= minBookingTime;
                })
                .WithMessage("Appointments must be booked at least 2 hours in advance.")
                .OverridePropertyName("StartTime");

            // Validación cruzada: Que la hora de FIN caiga dentro del horario laboral
            RuleFor(x => x)
                .Must(x =>
                {
                    var endTime = x.StartTime.AddMinutes(x.Duration);
                    return BeWithinBusinessHours(endTime);
                })
                .WithMessage("The appointment duration extends beyond working hours.")
                .OverridePropertyName("Duration");
        }

        private bool BeWithinBusinessHours(DateTime dateTime)
        {
            var day = dateTime.DayOfWeek;
            var time = dateTime.TimeOfDay;

            if (day == DayOfWeek.Sunday)
                return false;

            if (day == DayOfWeek.Saturday)
                // Sábados 8:00 AM - 12:00 PM
                return time >= TimeSpan.FromHours(8) && time <= TimeSpan.FromHours(12);

            // Lunes a Viernes 8:00 AM - 5:00 PM
            // CORRECCIÓN 3: Ajustado a 17.0 (5 PM) para coincidir con el mensaje
            // Usamos <= para permitir que una cita termine exactamente a las 5:00 PM
            return time >= TimeSpan.FromHours(8) && time <= TimeSpan.FromHours(17);
        }
    }
}















//using Application.DTOs.Appointment;
//using FluentValidation;
//using System;

//namespace Application.Validators.Appointment
//{
//    public class AppointmentCreateDtoValidator : AbstractValidator<AppointmentCreateDto>
//    {
//        private static readonly DateTime Now = DateTime.Now;
//        private static readonly DateTime TwoHoursFromNow = Now.AddHours(2.5);

//        public AppointmentCreateDtoValidator()
//        {
//            RuleFor(x => x.PatientId)
//                .GreaterThan(0)
//                .WithMessage("You must select a patient.");
//            RuleFor(x => x.EmployeeId)
//                .GreaterThan(0)
//                .WithMessage("You must select a doctor.");
//            RuleFor(x => x.Duration)
//                .InclusiveBetween(5, 150)
//                .WithMessage("The duration should be between 5 and 150 minutes.");

//            // StartTime: no vacío, no en el pasado, dentro de horario
//            RuleFor(x => x.StartTime)
//                .NotEmpty()
//                .WithMessage("The start date and time are required.")
//                .GreaterThanOrEqualTo(Now)
//                .WithMessage("Appointments cannot be scheduled in the past.")
//                .Must(BeWithinBusinessHours)
//                .WithMessage("The start time must be within the permitted hours (Mon-Fri 8:00-17:00, Sat 8:00-12:00).");

//            // Validación cruzada: cita termina >= 2.5 horas después de ahora + dentro del horario
//            RuleFor(x => x)
//                .Must(x =>
//                {
//                    var endTime = x.StartTime.AddMinutes(x.Duration);
//                    return endTime >= TwoHoursFromNow;
//                })
//                .WithMessage("The appointment must end at least 2 hours after the current time.")
//                .OverridePropertyName("StartTime"); 

//            RuleFor(x => x)
//                .Must(x =>
//                {
//                    var endTime = x.StartTime.AddMinutes(x.Duration);
//                    return BeWithinBusinessHours(endTime);
//                })
//                .WithMessage("The end time must be within the allowed time.")
//                .OverridePropertyName("Duration");
//        }
//        private static bool BeWithinBusinessHours(DateTime dateTime)
//        {
//            var day = dateTime.DayOfWeek;
//            var time = dateTime.TimeOfDay;

//            if (day == DayOfWeek.Sunday)
//                return false;

//            if (day == DayOfWeek.Saturday)
//                return time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(12);

//            // Lunes a viernes
//            return time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(17.5);
//        }
//    }
//}