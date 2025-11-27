using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Prescription
{
    public int Id { get; set; }

    public int? ConsultationId { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Consultation? Consultation { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
