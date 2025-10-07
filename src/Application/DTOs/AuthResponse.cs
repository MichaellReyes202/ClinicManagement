// Devuelve el resultado de una autenticación exitosa
namespace Application.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }

        // Lista de roles del usuario
        public List<string> Roles { get; set; } = new();
    }
}
