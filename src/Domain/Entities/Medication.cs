using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Medication
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? GenericName { get; set; }

    public string? Presentation { get; set; }

    public string? Concentration { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
