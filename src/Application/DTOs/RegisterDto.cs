
using System.ComponentModel.DataAnnotations;

// Contiene la información para registrar un nuevo usuario
namespace Application.DTOs
{
    public class RegisterDto : LoginDto
    {
        public required string ConfirmEmail { get; set; }

    }
}
