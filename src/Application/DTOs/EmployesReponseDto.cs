using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class EmployesReponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; } // 
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;

        public string? SecondLastName { get; set; }
        public int? PositionId { get; set; }

        public string? ContactPhone { get; set; }
        public DateOnly? HireDate { get; set; }

        public string? Dni { get; set; }

        public bool? IsActive { get; set; }

        public int? SpecialtyId { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string Email { get; set; } = null!;

        public string NormalizedEmail { get; set; } = null!;

    }
}
