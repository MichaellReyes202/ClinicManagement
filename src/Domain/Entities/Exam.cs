using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Exam
{
    public int Id { get; set; }

    public int? AppointmentId { get; set; }

    public int? ConsultationId { get; set; }

    public int ExamTypeId { get; set; }

    public string? Results { get; set; }

    public int? PerformedByEmployeeId { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Consultation? Consultation { get; set; }

    public virtual ExamType ExamType { get; set; } = null!;

    public virtual Employee? PerformedByEmployee { get; set; }

    public virtual CatExamsStatus Status { get; set; } = null!;
}
