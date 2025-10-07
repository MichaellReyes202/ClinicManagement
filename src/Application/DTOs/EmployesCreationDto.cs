using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class EmployesCreationDto
    {
        public required string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public required string LastName { get; set; } = null!;
        public string? SecondLastName { get; set; }
        public required int PositionId { get; set; } // Cargo que ocupa el empleado
        public string? ContactPhone { get; set; }
        public DateTime HireDate { get; set; } // Fecha de contratación
        public string? Dni { get; set; } // numero de cedula de identidad de nicaragua
        public int? SpecialtyId { get; set; } // Especialidad a la que pertenece el empleado
        public string Email { get; set; } = null!; // Correo personal del emplado 
    }
}
