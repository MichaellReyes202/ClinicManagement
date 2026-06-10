using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ClinicSchedule
{
    public int Id { get; set; }

    public short DayOfWeek { get; set; }

    public string DayName { get; set; } = null!;

    public bool IsOpen { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual User? UpdatedByUser { get; set; }
}
