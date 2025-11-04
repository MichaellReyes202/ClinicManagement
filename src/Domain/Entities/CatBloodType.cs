using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatBloodType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
