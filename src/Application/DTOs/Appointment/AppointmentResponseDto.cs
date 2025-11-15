using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;
public class AppointmentResponseDto
{
    public int Id { get; set; }
    public int? PatientId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime StartTime { get; set; }
    public int Duration { get; set; }
    public DateTime? EndTime { get; set; }
    public int StatusId { get; set; }
    public string? Reason { get; set; }
}
