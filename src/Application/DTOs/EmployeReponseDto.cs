using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class EmployeReponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string? SecondLastName { get; set; }
        public int Age { get; set; }
        public int? PositionId { get; set; }
        public int? SpecialtyId { get; set; }
        public string? ContactPhone { get; set; }
        public DateOnly? HireDate { get; set; }
        public string? Dni { get; set; }
        public string Email { get; set; } = null!;
        public bool? IsActive { get; set; }
    }
}
