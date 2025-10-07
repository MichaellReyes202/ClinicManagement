using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



//  Implementa IUserService. Se encargará de la lógica
//  relacionada con la gestión de
//  usuarios y sus roles.
namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;

        public UserService ( UserManager<User> userManager, IHttpContextAccessor httpContextAccessor , IUserRepository userRepository)
        {
            this._userManager = userManager;
            this._httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        // TODO : Implementar la paginacion 
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                Console.WriteLine("User is not authenticated");
                return null;
            }

            var emailClaim = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                Console.WriteLine("Email claim not found");
                return null;
            }

            var email = emailClaim.Value;
            // Log: $"Email encontrado: {email}"
            return await _userManager.FindByEmailAsync(email);
        }
    }
}
