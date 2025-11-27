using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Consultation
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int EmployeeId { get; set; }

    public int? AppointmentId { get; set; }

    public string? Reason { get; set; }

    public string? PhysicalExam { get; set; }

    public string? Diagnosis { get; set; }

    public string? TreatmentNotes { get; set; }

    public bool? IsFinalized { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual User? UpdatedByUser { get; set; }
}
