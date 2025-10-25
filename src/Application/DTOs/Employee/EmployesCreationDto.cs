using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Employee;
public class EmployesCreationDto
{
    public required string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public required string LastName { get; set; } = null!;
    public string? SecondLastName { get; set; }
    public required int Age { get; set; }

    public required int PositionId { get; set; } // Cargo que ocupa el empleado
    public string? ContactPhone { get; set; }
    public DateTime HireDate { get; set; } 
    public required string Dni { get; set; } 
    public int? SpecialtyId { get; set; } 
    public string Email { get; set; } = null!; 
}
