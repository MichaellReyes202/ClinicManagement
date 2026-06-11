using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class RoleView
{
    public int RoleId { get; set; }

    public int ViewId { get; set; }

    public int? GrantedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? GrantedByUser { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual CatView View { get; set; } = null!;
}
