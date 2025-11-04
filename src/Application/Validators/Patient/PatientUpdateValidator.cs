
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
           .WithMessage("El Id debe ser mayor que cero.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Matches(nameRegex).WithMessage("El nombre solo puede contener letras, incluyendo acentos");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("El segundo nombre no puede exceder 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]*$").WithMessage("El segundo nombre solo puede contener letras, incluyendo acentos")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido")
            .MinimumLength(2).WithMessage("El apellido debe tener al menos 2 caracteres")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres")
            .Matches(nameRegex).WithMessage("El apellido solo puede contener letras, incluyendo acentos");

        RuleFor(x => x.SecondLastName)
            .MaximumLength(100).WithMessage("El segundo apellido no puede exceder 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]*$").WithMessage("El segundo apellido solo puede contener letras, incluyendo acentos")
            .When(x => !string.IsNullOrEmpty(x.SecondLastName));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("La fecha de nacimiento debe ser anterior a hoy");

        RuleFor(x => x.Dni)
            .Must(dni => string.IsNullOrEmpty(dni) || nicaraguaCedulaRegex.IsMatch(dni))
            .WithMessage("La cédula debe tener el formato 000-000000-0000A");

        RuleFor(x => x.ContactPhone)
            .Matches(phoneRegex).WithMessage("El teléfono debe tener exactamente 8 dígitos numéricos")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("El correo debe ser válido")
            .MaximumLength(255).WithMessage("El correo no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.SexId)
            .NotNull().WithMessage("El sexo es requerido");

        RuleFor(x => x.BloodTypeId)
            .GreaterThan(0).WithMessage("El tipo de sangre debe ser un número válido")
            .When(x => x.BloodTypeId.HasValue);

        RuleFor(x => x.ConsultationReasons)
            .MaximumLength(1000).WithMessage("Los motivos de la consulta no pueden exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ConsultationReasons));

        RuleFor(x => x.ChronicDiseases)
            .MaximumLength(1000).WithMessage("Las enfermedades crónicas no pueden exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ChronicDiseases));

        RuleFor(x => x.Allergies)
            .MaximumLength(1000).WithMessage("Las alergias no pueden exceder 1000 caracteres")
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
            .WithMessage("La cédula no coincide con la fecha de nacimiento (DDMMAA)")
            .WithName(x => nameof(x.DateOfBirth));
    }
}
