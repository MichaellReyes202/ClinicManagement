using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Specialty
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<ExamType> ExamTypes { get; set; } = new List<ExamType>();
}
