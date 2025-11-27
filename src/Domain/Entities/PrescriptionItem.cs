using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PrescriptionItem
{
    public int Id { get; set; }

    public int? PrescriptionId { get; set; }

    public int? MedicationId { get; set; }

    public string Dose { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string Duration { get; set; } = null!;

    public int TotalQuantity { get; set; }

    public string? Instructions { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Medication? Medication { get; set; }

    public virtual Prescription? Prescription { get; set; }
}
