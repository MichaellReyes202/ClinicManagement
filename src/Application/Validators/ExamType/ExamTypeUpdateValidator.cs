
using Application.DTOs.ExamType;
using FluentValidation;

namespace Application.Validators.ExamType;

public class ExamTypeUpdateValidator : AbstractValidator<ExamTypeUpdateDto>
{
    public ExamTypeUpdateValidator()
    {
        Include(new ExamTypeCreateValidator());

        RuleFor(x => x.Id)
           .GreaterThan(0)
           .WithMessage("The Id must be greater than zero.");
        RuleFor(x => x.IsActive)
                .NotNull().WithMessage("IsActive is required.");
    }
}
