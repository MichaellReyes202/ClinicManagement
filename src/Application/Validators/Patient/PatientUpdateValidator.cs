
using Application.DTOs.Patient;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Validators.Patient;
public class PatientUpdateValidator : AbstractValidator<PatientUpdateDto>
{
    private readonly Regex nameRegex = new Regex(@"^[a-zA-ZÀ-ÿ\s]+$");
    private readonly Regex phoneRegex = new Regex(@"^\d{8}$");
    private readonly Regex nicaraguaCedulaRegex = new Regex(@"^[0-9]{3}-[0-9]{6}-[0-9]{4}[A-Z]$");
    public PatientUpdateValidator()
    {
        RuleFor(x => x.Id)
           .GreaterThan(0)
           .WithMessage("The Id must be greater than zero.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("The name is required")
            .MinimumLength(2).WithMessage("The name must have at least 2 characters")
            .MaximumLength(100).WithMessage("The name cannot exceed 100 characters")
            .Matches(nameRegex).WithMessage("The name can only contain letters, including accents");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("The middle name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]*$").WithMessage("The middle name can only contain letters, including accents")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("The last name is required")
            .MinimumLength(2).WithMessage("The last name must have at least 2 characters")
            .MaximumLength(100).WithMessage("The last name cannot exceed 100 characters")
            .Matches(nameRegex).WithMessage("The last name can only contain letters, including accents");

        RuleFor(x => x.SecondLastName)
            .MaximumLength(100).WithMessage("The second surname cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]*$").WithMessage("The second surname can only contain letters, including accents")
            .When(x => !string.IsNullOrEmpty(x.SecondLastName));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("The date of birth must be before today");

        RuleFor(x => x.Dni)
            .Must(dni => string.IsNullOrEmpty(dni) || nicaraguaCedulaRegex.IsMatch(dni))
            .WithMessage("The ID must have the format 000-000000-0000A");

        RuleFor(x => x.ContactPhone)
            .Matches(phoneRegex).WithMessage("The phone number must have exactly 8 numeric digits")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("The email must be valid")
            .MaximumLength(255).WithMessage("The email cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("The address cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.SexId)
            .NotNull().WithMessage("Sex is required");

        RuleFor(x => x.BloodTypeId)
            .GreaterThan(0).WithMessage("The blood type must be a valid number")
            .When(x => x.BloodTypeId.HasValue);

        RuleFor(x => x.ConsultationReasons)
            .MaximumLength(1000).WithMessage("The reasons for the consultation cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.ConsultationReasons));

        RuleFor(x => x.ChronicDiseases)
            .MaximumLength(1000).WithMessage("Chronic diseases cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.ChronicDiseases));

        RuleFor(x => x.Allergies)
            .MaximumLength(1000).WithMessage("Allergies cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Allergies));

        // Validación del tutor
        When(x => x.Guardian != null, () =>
        {
            RuleFor(x => x.Guardian).SetValidator(new PatientGuardianValidator());
        });

        // Validación de coincidencia fecha de nacimiento y cédula
        RuleFor(x => x)
            .Must(x =>
            {

                if (string.IsNullOrEmpty(x.Dni)) return true;
                string yearPart = (x.DateOfBirth.Year % 100).ToString("D2");

                string expected = x.DateOfBirth.Day.ToString("D2") +
                                  x.DateOfBirth.Month.ToString("D2") +
                                  yearPart; // Usamos el valor corregido

                if (x.Dni.Length >= 10)
                {
                    string cedulaPart = x.Dni.Substring(4, 6);
                    return cedulaPart == expected;
                }

                return true;
            })
            .WithMessage("The ID does not match the date of birth (DDMMYY)")
            .WithName(x => nameof(x.DateOfBirth));
    }
}
