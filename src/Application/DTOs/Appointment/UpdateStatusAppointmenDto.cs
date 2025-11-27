using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;
public class UpdateStatusAppointmenDto
{
    public int AppointmenId { get; set; }
    public int StatusId { get; set; }
}
