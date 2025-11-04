using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Patient;
public class PatientUpdateDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string? SecondLastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? Dni { get; set; }

    // Contacto
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }

    // Detalles extendidos
    public int? SexId { get; set; }
    public int? BloodTypeId { get; set; }

    // Antecedentes médicos
    public string? ConsultationReasons { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? Allergies { get; set; }

    public PatientGuardianDto? Guardian { get; set; }
}