

namespace Application.DTOs.Appointment;
public class AppointmentUpdateDto : AppointmentCreateDto
{
    public int Id { get; set; }
    public int StatusId { get; set; }
}
