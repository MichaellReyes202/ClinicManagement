using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Appointment
{
    public int Id { get; set; }

    public int? PatientId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime StartTime { get; set; }

    public int Duration { get; set; }

    public DateTime? EndTime { get; set; }

    public int StatusId { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public virtual Consultation? Consultation { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual Patient? Patient { get; set; }

    public virtual CatAppointmentStatus Status { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
