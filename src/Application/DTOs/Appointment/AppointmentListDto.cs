using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;
public class AppointmentListDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientFullName { get; set; } = null!;
    public int EmployeeId { get; set; } // id del doctor 
    public int? DoctorSpecialtyId { get; set; } // especialidad del doctor
    public string DoctorFullName { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Duration { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = null!;
    public int StatusId { get; set; }
}
