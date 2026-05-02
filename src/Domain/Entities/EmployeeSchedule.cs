using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class EmployeeSchedule
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public short DayOfWeek { get; set; }

    public bool IsAvailable { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
