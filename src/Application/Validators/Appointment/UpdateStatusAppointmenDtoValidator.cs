
using Application.DTOs.Appointment;
using FluentValidation;

namespace Application.Validators.Appointment;
public class UpdateStatusAppointmenDtoValidator : AbstractValidator<UpdateStatusAppointmenDto>
{
    public UpdateStatusAppointmenDtoValidator()
    {
        RuleFor(x => x.AppointmenId).NotNull().GreaterThan(0).WithMessage("appointment id is required");
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("Appointment status is required");
    }
}
