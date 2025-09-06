using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Application.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;


//  Implementa IAuthService. Contendrá la lógica para verificar
//  credenciales, generar tokens JWT y registrar usuarios,
//  interactuando con los repositorios.

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;

        public AuthService
        (
            UserManager<User> userManager,
            SignInManager<User> signInManager ,
            IHttpContextAccessor contextAccessor ,
            IConfiguration configuration
        )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._contextAccessor = contextAccessor;
            this._configuration = configuration;
        }
        public async Task<AuthenticatedUserDto> RegisterAsync(RegisterDto registerDto)
        {
            var user = new User
            {
                Email = registerDto.Email
                // Set other properties as needed
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            return 

            //if (!result.Succeeded)
            //{
            //    var errors = result.Errors.Select(e => e.Description).ToList();
            //    throw new InvalidOperationException(string.Join(", ", errors));
            //}
            //return await GenerateJwtTokenAsync(user);
        }
        public Task<string> LoginAsync(string email, string password)
        {
            throw new NotImplementedException();
        }

        public async Task<AuthenticatedUserDto> GenerateJwtTokenAsync(User user)
        {
            // Find the user by email using the UserManager
            var userCreate = await _userManager.FindByEmailAsync(user.Email);

            if (user == null)
            {
                throw new InvalidOperationException("User not found after registration.");
            }

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Email, userCreate.Email!));


            // Get the user's roles through the UserManager, which delegates to your UserStore
            var roles = await _userManager.GetRolesAsync(user);

            // Add each role as a 'Role' claim
            foreach (var rol in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            // Configure the key and credentials for token signing
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(1);

            var tokenDeSeguridad = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new AuthenticatedUserDto
            {
                Token = token,
                Expiration = expiration
            };
            
        }

        public Task<User?> GetCurrentUserAsync()
        {
            var emailClaim = _contextAccessor.HttpContext?
                .User
                .Claims
                .Where(x => x.Type == ClaimTypes.Email).FirstOrDefault();
            if(emailClaim is  null)
            {
                return null;
            }
            var email = emailClaim.Value;
            return _userManager.FindByEmailAsync(email);
        }

        public Task<AuthenticatedUserDto> RegisterAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticatedUserDto> GenerateJwtTokenAsync(RegisterDto register)
        {
            throw new NotImplementedException();
        }
    }
}
