using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;
public class AppointmentUpdateDto : AppointmentCreateDto
{
    public int Id { get; set; }
    public int StatusId { get; set; }
}
