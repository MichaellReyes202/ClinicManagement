using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Appointment;
public class AppointmentFilterDto
{
    public string? Search { get; set; }      // Coincide con 'search'
    public int? Specialty { get; set; }      // Coincide con 'specialty'
    public int? Doctor { get; set; }         // Coincide con 'doctor'
    public int? Status { get; set; }         // Coincide con 'status'
    public DateTime? DateFrom { get; set; }  // Coincide con 'dateFrom'
    public DateTime? DateTo { get; set; }    // Coincide con 'dateTo'
}
