
using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class EmployesCreationDtoValidator : AbstractValidator<EmployesCreationDto>
    {
        public EmployesCreationDtoValidator()
        {
            // FirstName
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

            // MiddleName
            RuleFor(x => x.MiddleName)
                .MaximumLength(100).WithMessage("Middle name must not exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

            // LastName
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

            // SecondLastName
            RuleFor(x => x.SecondLastName)
                .MaximumLength(100).WithMessage("Second last name must not exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.SecondLastName));

            // PositionId
            RuleFor(x => x.PositionId)
                .NotNull().WithMessage("Position is required.")
                .GreaterThan(0).WithMessage("Position ID must be greater than zero.");

            // ContactPhone
            RuleFor(x => x.ContactPhone)
                .Matches(@"^[0-9+\-\s]+$").WithMessage("Contact phone can only contain numbers, spaces, and + or - signs.")
                .MaximumLength(50).WithMessage("Contact phone must not exceed 50 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));

            // HireDate
            RuleFor(x => x.HireDate)
        .NotEmpty().WithMessage("Hire date is required.")
        .LessThanOrEqualTo(DateTime.Now).WithMessage("Hire date cannot be in the future.");


            // Dni (Nicaraguan ID)
            RuleFor(x => x.Dni)    //                                                                          401-101198-1003A
                .Matches(@"^\d{3}-\d{6}-\d{4}[A-Z]$").WithMessage("DNI must follow the Nicaraguan format (e.g. 001-010101-0000A).")
                .MaximumLength(14).WithMessage("\r\nDNI cannot be more than 16 characters (including hyphens)")
                .When(x => !string.IsNullOrWhiteSpace(x.Dni));

            // SpecialtyId
            RuleFor(x => x.SpecialtyId)
                .GreaterThan(0).WithMessage("Specialty ID must be greater than zero.")
                .When(x => x.SpecialtyId.HasValue);

            // Email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
        }
    }
}
