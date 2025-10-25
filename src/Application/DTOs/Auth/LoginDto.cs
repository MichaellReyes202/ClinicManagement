using System.ComponentModel.DataAnnotations;


// Contiene los datos que un usuario envía para iniciar sesión 
namespace Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255, ErrorMessage = "El correo excede la cantidad de caracteres ")]
        public required string Email { get; set; } = string.Empty;
        [Required]
        [MinLength(6)]
        public required string Password { get; set; }
    }
}
