using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatExamsStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
