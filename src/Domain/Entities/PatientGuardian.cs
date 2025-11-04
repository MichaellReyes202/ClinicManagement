using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PatientGuardian
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Dni { get; set; }

    public string Relationship { get; set; } = null!;

    public string? ContactPhone { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
