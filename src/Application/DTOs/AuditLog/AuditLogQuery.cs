using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AuditLog;
public class AuditLogQuery
{
    public string? StatusName { get; set; }
    public string? ModuleName { get; set; }
    public string? ActionName { get; set; }
    public string? SearchTerm { get; set; }
    public int limit { get; set; } = 1;
    public int offset { get; set; } = 20;
}