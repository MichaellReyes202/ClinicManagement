

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Appointment;
public class AppointmentCreateDto
{
    public int PatientId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartTime { get; set; }
    public int Duration { get; set; } = 30;
    public string? Reason { get; set; }
}
