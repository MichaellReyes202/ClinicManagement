using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Patient;
public class PatientSearchDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public  string? Dni { get; set; }
    public string? ContactPhone { get; set; }

}
