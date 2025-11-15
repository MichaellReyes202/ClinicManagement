using Application.DTOs.Patient;
using FluentValidation;
using System.Text.RegularExpressions;


namespace Application.Validators.Patient;

public class PatientCreateValidator : AbstractValidator<PatientCreateDto>
{
    private readonly Regex _nameRegex = new(@"^[a-zA-ZÀ-ÿ\s]+$");
    private readonly Regex _phoneRegex = new(@"^\d{8}$");
    private readonly Regex _nicaraguaCedulaRegex = new(@"^[0-9]{3}-[0-9]{6}-[0-9]{4}[A-Z]$");

    public PatientCreateValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("The name is required")
            .MinimumLength(2).WithMessage("The name must be at least 2 characters")
            .MaximumLength(100).WithMessage("The name cannot exceed 100 characters")
            .Matches(_nameRegex).WithMessage("The name can only contain letters, including accents");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]*$").WithMessage("The middle name can only contain letters, including accents")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters")
            .MaximumLength(100).WithMessage("The last name cannot exceed 100 characters")
            .Matches(_nameRegex).WithMessage("The last name can only contain letters, including accents");

        RuleFor(x => x.SecondLastName)
            .MaximumLength(100).WithMessage("The second last name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]*$").WithMessage("The second last name can only contain letters, including accents")
            .When(x => !string.IsNullOrEmpty(x.SecondLastName));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Now))
            .WithMessage("Date of birth must be before today");

        RuleFor(x => x.Dni)
            .Must(dni => string.IsNullOrEmpty(dni) || _nicaraguaCedulaRegex.IsMatch(dni))
            .WithMessage("The ID must have the format 000-000000-0000A");

        RuleFor(x => x)
            .Must(x =>
            {
                if (string.IsNullOrEmpty(x.Dni)) return true;
                if (!_nicaraguaCedulaRegex.IsMatch(x.Dni)) return true;

                string yearPart = (x.DateOfBirth.Year % 100).ToString("D2");
                string expected = $"{x.DateOfBirth.Day:D2}{x.DateOfBirth.Month:D2}{yearPart}";
                string cedulaPart = x.Dni.Substring(4, 6);
                return cedulaPart == expected;
            })
            .WithMessage("The ID does not match the date of birth (DDMMYY)")
            .WithName("Dni");

        RuleFor(x => x.ContactPhone)
            .Matches(_phoneRegex).WithMessage("The phone must have exactly 8 numerical digits")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("The email must be valid")
            .MaximumLength(255).WithMessage("The email cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("The address cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.SexId)
            .NotNull().WithMessage("sex is required")
            .GreaterThan(0).WithMessage("The gender ID must be a valid number");

        RuleFor(x => x.BloodTypeId)
            .GreaterThan(0).WithMessage("Blood type ID must be a valid number")
            .When(x => x.BloodTypeId.HasValue);

        RuleFor(x => x.ConsultationReasons)
            .MaximumLength(250).WithMessage("The reasons for the query cannot exceed 250 characters")
            .When(x => !string.IsNullOrEmpty(x.ConsultationReasons));

        RuleFor(x => x.ChronicDiseases)
            .MaximumLength(250).WithMessage("Chronic diseases cannot exceed 250 characters")
            .When(x => !string.IsNullOrEmpty(x.ChronicDiseases));

        RuleFor(x => x.Allergies)
            .MaximumLength(250).WithMessage("Allergies cannot exceed 250 characters")
            .When(x => !string.IsNullOrEmpty(x.Allergies));

        When(x => x.Guardian != null, () =>
        {
            RuleFor(x => x.Guardian.FullName)
                .NotEmpty().WithMessage("The guardian's full name is required")
                .MinimumLength(2).WithMessage("Guardian name must be at least 2 characters")
                .MaximumLength(200).WithMessage("Guardian name cannot exceed 200 characters")
                .Matches(_nameRegex).WithMessage("The tutor's name can only contain letters, including accents");

            RuleFor(x => x.Guardian.Relationship)
                .NotEmpty().WithMessage("Relationship is required")
                .MinimumLength(2).WithMessage("The relationship must be at least 2 characters")
                .MaximumLength(100).WithMessage("The relationship cannot exceed 100 characters");

            RuleFor(x => x.Guardian.Dni)
                .Must(dni => string.IsNullOrEmpty(dni) || _nicaraguaCedulaRegex.IsMatch(dni))
                .WithMessage("The guardian's ID must have the format 000-000000-0000A")
                .When(x => !string.IsNullOrEmpty(x.Guardian.Dni));

            RuleFor(x => x.Guardian.ContactPhone)
                .Matches(_phoneRegex).WithMessage("The tutor's phone number must have exactly 8 digits")
                .When(x => !string.IsNullOrEmpty(x.Guardian.ContactPhone));

            RuleFor(x => x.Guardian)
                .Must(g =>
                {
                    var values = new[] { g.FullName, g.Relationship, g.Dni, g.ContactPhone };
                    bool allEmpty = values.All(v => string.IsNullOrWhiteSpace(v));
                    bool allFilled = values.All(v => !string.IsNullOrWhiteSpace(v));
                    return allEmpty || allFilled;
                })
                .WithMessage("You must complete all tutor fields or leave them empty")
                .OverridePropertyName("Guardian");
        });

        RuleFor(x => x)
            .Must(x =>
            {
                if (x.Guardian != null) return true;
                var today = DateOnly.FromDateTime(DateTime.Now);
                int age = today.Year - x.DateOfBirth.Year;
                if (x.DateOfBirth > today.AddYears(-age)) age--;
                return age >= 18;
            })
            .WithMessage("If the patient is a minor, a guardian must be registered")
            .WithName("Guardian");

        RuleFor(x => x)
            .Must(x =>
            {
                if (x.Guardian == null) return true;
                var today = DateOnly.FromDateTime(DateTime.Now);
                int age = today.Year - x.DateOfBirth.Year;
                if (x.DateOfBirth > today.AddYears(-age)) age--;
                return age < 18;
            })
            .WithMessage("If the patient is of legal age, he or she should not have a guardian.")
            .WithName("Guardian");
    }
}