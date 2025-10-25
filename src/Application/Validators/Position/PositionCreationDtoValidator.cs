using Application.DTOs.Position;
using FluentValidation;

namespace Application.Validators.Position
{
    public class PositionCreationDtoValidator : AbstractValidator<PositionCreationDto>
    {
        public PositionCreationDtoValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(5).WithMessage("Name must be at least 5 characters long.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");
        }
    }
}
