
using Application.DTOs.ExamType;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators.ExamType;
public class ExamTypeCreateValidator : AbstractValidator<ExamTypeCreateDto>
{
    private readonly Regex nameRegex = new Regex(@"^[a-zA-ZÀ-ÿ\s]+$");
    public ExamTypeCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The name is required")
            .MinimumLength(2).WithMessage("The name must have at least 2 characters")
            .MaximumLength(100).WithMessage("The name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(100).WithMessage("The description cannot exceed 250 characters");

        RuleFor( x => x.DeliveryTime)
            .NotNull()
            .GreaterThan(1)
            .LessThan(240)
            .WithMessage("Delivery time should be between 1 and 240 hours. ");
        RuleFor(x => x.PricePaid)
            .GreaterThan(1)
            .LessThan(3_000)
            .WithMessage("The exam fee must be between 1 and 1,000.");

        RuleFor(x => x.SpecialtyId)
            .NotNull().GreaterThan(0)
            .WithMessage("SpecialtyId must be a valid value.");

    }
}
