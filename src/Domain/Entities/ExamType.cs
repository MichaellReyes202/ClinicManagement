using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ExamType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DeliveryTime { get; set; }

    public decimal PricePaid { get; set; }

    public int SpecialtyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public bool IsActive { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual Specialty Specialty { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
