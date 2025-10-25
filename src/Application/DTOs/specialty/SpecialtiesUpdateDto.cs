using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.specialty
{
    public class SpecialtiesUpdateDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required bool IsActive { get; set; }

    }
}
