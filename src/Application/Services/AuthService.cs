using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


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
        private readonly IValidator<LoginDto> _validatorLogin;
        private readonly IValidator<RegisterDto> validatorRegister;

        public AuthService
        (
            UserManager<User> userManager,
            SignInManager<User> signInManager ,
            IHttpContextAccessor contextAccessor ,
            IConfiguration configuration ,
            IValidator<LoginDto> validator_login,
            IValidator<RegisterDto> validatorRegister
        )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._contextAccessor = contextAccessor;
            this._configuration = configuration;
            this._validatorLogin = validator_login;
            this.validatorRegister = validatorRegister;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginDto loginDto)
        {
            var validationResult = await _validatorLogin.ValidateAsync(loginDto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<AuthResponse>.Failure(errors);
            }

            var user = await _userManager.FindByNameAsync(loginDto.Email);
            if (user is null)
            {
                return Result<AuthResponse>.Failure("Invalid credentials", "InvalidCredentials");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                // Verificar si el bloqueo ha expirado
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd.HasValue && lockoutEnd.Value <= DateTimeOffset.UtcNow)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Result<AuthResponse>.Failure("User account is locked out", "UserLockedOut");
            }
            var result = await _signInManager
                .CheckPasswordSignInAsync(user, loginDto.Password,lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return Result<AuthResponse>.Success(await GenerateJwtTokenAsync(user.Email));
            }
            else
            {
                return Result<AuthResponse>.Failure("Invalid credentials", "InvalidCredentials");
            }
        }


        public async Task<Result<AuthResponse>> RegisterAsync(RegisterDto registerDto)
        {
            var validationResult = await validatorRegister.ValidateAsync(registerDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<AuthResponse>.Failure(errors);
            }

            // Verificar si el usuario ya existe
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return Result<AuthResponse>.Failure("User with this email already exists", "Email Exists");
            }

            var user = new User{ Email = registerDto.Email};

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                // Asignar el rol "User" al nuevo usuario
                //await _userManager.AddToRoleAsync(user, "User");
                var authResponse = await GenerateJwtTokenAsync(user.Email);
                return Result<AuthResponse>.Success(authResponse);
            }
            else
            {
                var errors = result.Errors
                    .Select(e => new ValidationError(string.Empty, e.Description))
                    .ToList();
                return Result<AuthResponse>.Failure(errors);
            }

        }
       
        public async Task<AuthResponse> GenerateJwtTokenAsync(string email)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, email)
            };

            // buscar al usuario por email 
            User? user = await _userManager.FindByEmailAsync(email)
                ?? throw new Exception("user not found");

            // Obtener los roles del usuario 
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var jwtSettings = _configuration.GetSection("JwtSettings");

            // configuracion de la lleva y credenciales para la firma del token 
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(1);

            var tokenDeSeguridad = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new AuthResponse
            {
                Token = token,
                Expiration = expiration
            };
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var emailClaim = _contextAccessor
                .HttpContext!
                .User
                .Claims.Where(x => x.Type == ClaimTypes.Email).FirstOrDefault();
            if(emailClaim is null)
            {
                return null;
            }
            var email = emailClaim.Value;
            return await _userManager.FindByEmailAsync(email);
        }

        
    }
}
