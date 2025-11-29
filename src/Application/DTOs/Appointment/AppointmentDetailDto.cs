using Application.DTOs.Patient;

namespace Application.DTOs.Appointment;

public class AppointmentDetailDto
{
    public int AppointmentId { get; set; }
    public int StatusId { get; set; }
    public required PatientByAppointmentDto Patient { get; set; }
    public required string Doctor { get; set; }
    public int DoctorId { get; set; }
    public DateTime StartTime { get; set; }
    public string? Reason { get; set; }
}
