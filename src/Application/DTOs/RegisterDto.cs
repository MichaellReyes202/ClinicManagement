
using System.ComponentModel.DataAnnotations;

// Contiene la información para registrar un nuevo usuario
namespace Application.DTOs
{
    public class RegisterDto : LoginDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(length: 255)]
        // valida que el email de confirmación sea igual al email
        [Compare("Email", ErrorMessage = "The email and confirmation email do not match.")]
        public required string ConfirmEmail { get; set; }

    }
}
