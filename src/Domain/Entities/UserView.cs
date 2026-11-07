using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserView
{
    public int UserId { get; set; }

    public int ViewId { get; set; }

    public int? GrantedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? GrantedByUser { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual CatView View { get; set; } = null!;
}
