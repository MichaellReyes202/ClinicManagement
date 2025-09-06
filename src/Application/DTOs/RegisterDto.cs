
using System.ComponentModel.DataAnnotations;

// Contiene la información para registrar un nuevo usuario
namespace Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(length: 255)]
        public required string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
