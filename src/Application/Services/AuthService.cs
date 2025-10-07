using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
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
        private readonly IMapper _mapper;

        public AuthService
        (
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            IValidator<LoginDto> validator_login,
            IValidator<RegisterDto> validatorRegister,
            IMapper mapper
        )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._contextAccessor = contextAccessor;
            this._configuration = configuration;
            this._validatorLogin = validator_login;
            this.validatorRegister = validatorRegister;
            _mapper = mapper;
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
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.BadRequest , "Invalid Credentials"));
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
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.TooManyRequests, "User account is locked. Please try again later."));
            }
            var result = await _signInManager
                .CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.BadRequest, "Invalid Credentials"));
            }
            return Result<AuthResponse>.Success(await GenerateJwtTokenAsync(user));
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
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.Conflict, "User with this email already exists."));
            }
            var user = _mapper.Map<User>(registerDto);
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                // Asignar el rol "User" al nuevo usuario
                await _userManager.AddToRoleAsync(user, "User");
                var authResponse = await GenerateJwtTokenAsync(existingUser!);
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

        public async Task<AuthResponse> GenerateJwtTokenAsync(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Sub , user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            }; 
            var roles = await _userManager.GetRolesAsync(user);

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var jwtSettings = _configuration.GetSection("JwtSettings");

            // configuracion de la lleva y credenciales para la firma del token 
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(1);

            var tokenDeSeguridad = new JwtSecurityToken(
                 issuer: null ,// jwtSettings["Issuer"],
                 audience: null, // jwtSettings["Audience"],
                 claims: claims,
                 expires: expiration,
                 signingCredentials: credentials
             );
            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new AuthResponse
            {
                Token = token,
                Expiration = expiration,
                Roles = roles.ToList()
            };
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var emailClaim = _contextAccessor
                .HttpContext!
                .User
                .Claims.Where(x => x.Type == ClaimTypes.Email).FirstOrDefault();
            if (emailClaim is null)
            {
                return null;
            }
            var email = emailClaim.Value;
            return await _userManager.FindByEmailAsync(email);
        }


    }
}
