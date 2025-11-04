using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Employee
{
    public class EmployeeListDTO
    {
        public int Id { get; set; }
        public required string FullName { get; set; }
        public string? Dni { get; set; }
        public string PositionName { get; set; } 
        public string? EspecialtyName { get; set; } 
        public string? ContactPhone { get; set; }
        public string Email { get; set; }
        public bool? IsActive { get; set; }
        public int PositionId { get; set; }
        public int? SpecialtyId { get; set; }

    }
}
