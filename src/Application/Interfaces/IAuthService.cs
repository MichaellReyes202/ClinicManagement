using Domain.Errors;
using Domain.Entities;
using Application.DTOs.User;
using Application.DTOs.Auth;


// Define la interfaz para los métodos de autenticación
namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> LoginAsync(LoginDto loginDto);
        Task<Result<UserDto>> RegisterAsync(RegisterDto registerDto);

        // metodo para generar token JWT
        Task<AuthResponse> GenerateJwtTokenAsync(User user);
        Task<Result<AuthResponse>> GetUserOnly();
    }
}
