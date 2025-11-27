using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatAuditStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Auditlog> Auditlogs { get; set; } = new List<Auditlog>();
}
