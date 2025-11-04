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
        .WithMessage("El nombre completo del tutor debe tener entre 2 y 200 caracteres y solo puede contener letras, incluyendo acentos");

        RuleFor(x => x.Dni)
            .Must(dni => string.IsNullOrEmpty(dni) || nicaraguaCedulaRegex.IsMatch(dni))
            .WithMessage("La cédula del tutor debe tener el formato 000-000000-0000A");

        RuleFor(x => x.Relationship)
            .Must(r => string.IsNullOrEmpty(r) || (r.Length >= 2 && r.Length <= 100))
            .WithMessage("El parentesco debe tener entre 2 y 100 caracteres");

        RuleFor(x => x.ContactPhone)
            .Must(p => string.IsNullOrEmpty(p) || Regex.IsMatch(p, @"^\d{8}$"))
            .WithMessage("El teléfono del tutor debe tener exactamente 8 dígitos numéricos");

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
            .WithMessage("Por favor complete todos los campos del tutor si llena alguno.");
    }
}
