using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Patient;
public class PatientByAppointmentDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public int Age { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
}
