
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    // Dto para crear una nueva especialidad (error message en ingles)
    public class SpecialtiesDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }

    }
}
