// Devuelve el resultado de una autenticación exitosa
namespace Application.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }
        public UserInfo User { get; set; } = null!;

    }
    public class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public int RoleId { get; set; }
        public int? EmployeeId { get; set; }

    }
}
