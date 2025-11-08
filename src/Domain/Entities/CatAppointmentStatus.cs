using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatAppointmentStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
