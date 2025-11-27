using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Promotion
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public decimal? FixedPrice { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool? IsActive { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<PromotionExam> PromotionExams { get; set; } = new List<PromotionExam>();
}
