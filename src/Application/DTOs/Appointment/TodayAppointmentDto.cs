using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;

public class TodayAppointmentDto
{
    public int Id { get; set; }
    public string TimeDisplay { get; set; } = null!; // "08:00 AM"
    public string PatientFullName { get; set; } = null!;
    public string PatientPhone { get; set; } = null!;
    public string DoctorFullName { get; set; } = null!;
    public string SpecialtyName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int StatusId { get; set; }
}
