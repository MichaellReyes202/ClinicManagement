using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;
public class DoctorAvailabilityDto
{
    public int DoctorId { get; set; }
    public string FullName { get; set; } = null!;
    public string SpecialtyName { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public DateTime? NextAppointmentTime { get; set; }
    public string NextAppointmentDisplay { get; set; } = null!;
    public int AvailableSlotsToday { get; set; }
    public int AppointmentsTodayCount { get; set; }
}
