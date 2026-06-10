using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatView
{
    public int Id { get; set; }

    public string Name { get; set; } = null!; 

    public string Route { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<UserView> UserViews { get; set; } = new List<UserView>();
}
