using Application.DTOs.Position;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators.Position
{
    public class PositionUpdateDtoValidator : AbstractValidator<PositionUpdateDto>
    {
        public PositionUpdateDtoValidator()
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
