using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Define la interfaz para los métodos de autenticación
namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(string email, string password);
        Task<AuthenticatedUserDto> RegisterAsync(User user);

        // metodo para generar token JWT
        Task<AuthenticatedUserDto> GenerateJwtTokenAsync(RegisterDto register);

        // metodo para obetner el usuario actual
        Task<User?> GetCurrentUserAsync();


    }
}
