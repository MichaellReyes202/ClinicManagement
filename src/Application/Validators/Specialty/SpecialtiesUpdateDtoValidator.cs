using Application.DTOs.specialty;
using FluentValidation;

namespace Application.Validators.Specialty
{
    public class SpecialtiesUpdateDtoValidator : AbstractValidator<SpecialtiesUpdateDto>
    {
        public SpecialtiesUpdateDtoValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(5).WithMessage("Name must be at least 5 characters long.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("IsActive is required.");

        }
    }
}
