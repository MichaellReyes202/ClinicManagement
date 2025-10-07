
using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class SpecialtiesDtoValidator : AbstractValidator<SpecialtiesDto>
    {
        public SpecialtiesDtoValidator()
        {
            RuleFor(RuleFor => RuleFor.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
            RuleFor(RuleFor => RuleFor.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(250).WithMessage("Description cannot exceed 500 characters.");
        }
    }
}
