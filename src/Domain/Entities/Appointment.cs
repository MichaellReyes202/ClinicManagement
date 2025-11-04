using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Appointment
{
    public int Id { get; set; }

    public int? EmployeeId { get; set; }

    public string AppointmentType { get; set; } = null!;

    public DateTime DateTime { get; set; }

    public TimeSpan? Duration { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee? Employee { get; set; }
}
