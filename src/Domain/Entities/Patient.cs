using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Patient
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string? SecondLastName { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string? Dni { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public string? Address { get; set; }

    public int SexId { get; set; }

    public int? BloodTypeId { get; set; }

    public string? ConsultationReasons { get; set; }

    public string? ChronicDiseases { get; set; }

    public string? Allergies { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual PatientGuardian? PatientGuardian { get; set; }
    public virtual CatBloodType? BloodType { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual CatSexo Sex { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
