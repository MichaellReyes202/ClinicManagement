using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PromotionExam
{
    public int PromotionId { get; set; }

    public int ExamTypeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ExamType ExamType { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
