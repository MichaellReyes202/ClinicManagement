using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Patient;
public class PatientGuardianDto
{
    public string FullName { get; set; } = null!;
    public string Relationship { get; set; } = null!;
    public string? Dni { get; set; }
    public string? ContactPhone { get; set; }
}
