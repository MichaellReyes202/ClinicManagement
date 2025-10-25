// Representa la información de un usuario que se mostrará en la API
namespace Application.DTOs.User;
public class UserDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
