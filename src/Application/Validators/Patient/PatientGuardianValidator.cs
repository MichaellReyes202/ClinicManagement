using Application.DTOs.Patient;
using FluentValidation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Validators.Patient;
public class PatientGuardianValidator: AbstractValidator<PatientGuardianDto>
{
    private readonly Regex nicaraguaCedulaRegex = new Regex(@"^[0-9]{3}-[0-9]{6}-[0-9]{4}[A-Z]$");
    public PatientGuardianValidator()
    {
        RuleFor(x => x.FullName)
        .Must(name => string.IsNullOrEmpty(name) || (name.Length >= 2 && name.Length <= 200 && Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ\s]+$")))
        .WithMessage("The tutor's full name must be between 2 and 200 characters and can only contain letters, including accents");

        RuleFor(x => x.Dni)
            .Must(dni => string.IsNullOrEmpty(dni) || nicaraguaCedulaRegex.IsMatch(dni))
            .WithMessage("The guardian's ID must follow the format 000-000000-0000A");

        RuleFor(x => x.Relationship)
            .Must(r => string.IsNullOrEmpty(r) || (r.Length >= 2 && r.Length <= 100))
            .WithMessage("The relationship must be between 2 and 100 characters");

        RuleFor(x => x.ContactPhone)
            .Must(p => string.IsNullOrEmpty(p) || Regex.IsMatch(p, @"^\d{8}$"))
            .WithMessage("The tutor's phone number must have exactly 8 numeric digits");

        // Todos o ninguno
        RuleFor(x => x)
            .Must(g =>
            {
                var allEmpty = string.IsNullOrEmpty(g.FullName) &&
                                string.IsNullOrEmpty(g.Dni) &&
                                string.IsNullOrEmpty(g.Relationship) &&
                                string.IsNullOrEmpty(g.ContactPhone);

                var allFilled = !string.IsNullOrEmpty(g.FullName) &&
                                !string.IsNullOrEmpty(g.Dni) &&
                                !string.IsNullOrEmpty(g.Relationship) &&
                                !string.IsNullOrEmpty(g.ContactPhone);

                return allEmpty || allFilled;
            })
            .WithMessage("Please complete all the tutor fields if you fill in any.");
    }
}
