using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Auditlog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int? Performedbyuserid { get; set; }
    public int Moduleid { get; set; }
    public int Statusid { get; set; }
    public string Actiontype { get; set; } = null!;
    public int Recordid { get; set; }
    public string Recorddisplay { get; set; } = null!; // es para 
    public string? Changedetail { get; set; }
    public virtual CatAuditModule Module { get; set; } = null!;
    public virtual CatAuditStatus Status { get; set; } = null!;
    public virtual User? Performedbyuser { get; set; }
}

