using Application.DTOs;
using Application.Interfaces;
using Application.Util;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
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
        private readonly IEmployesRepository _employesRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;

        public AuthService
        (
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            IValidator<LoginDto> validator_login,
            IValidator<RegisterDto> validatorRegister,
            IMapper mapper , 
            IEmployesRepository employesRepository ,
            IRoleRepository roleRepository ,
            IUserRepository userRepository


        )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._contextAccessor = contextAccessor;
            this._configuration = configuration;
            this._validatorLogin = validator_login;
            this.validatorRegister = validatorRegister;
            _mapper = mapper;
            _employesRepository = employesRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
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

            var user = await _userRepository.GetUserWithEmployeeByEmailAsync(loginDto.Email);
            if (user is null)
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.BadRequest , "Invalid Credentials"));

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd.HasValue && lockoutEnd.Value <= DateTimeOffset.UtcNow)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }
                else
                {
                    return Result<AuthResponse>.Failure(new Error(ErrorCodes.TooManyRequests, "User account is locked. Please try again later."));
                }
            }

            if (await _userManager.IsLockedOutAsync(user))
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.TooManyRequests, "User account is locked. Please try again later."));

            var result = await _signInManager
                .CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
            
            if(result.IsLockedOut)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.TooManyRequests, "User account is locked due to multiple failed login attempts. Please try again later."));
            }   
            if( result.IsNotAllowed)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "User is not allowed to sign in. Please contact support."));
            }
            if (!result.Succeeded)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.BadRequest, "Invalid Credentials"));
            }
            else
            {
                return Result<AuthResponse>.Success(await GenerateJwtTokenAsync(user));
            }
                


        }


        public async Task<Result<UserDto>> RegisterAsync(RegisterDto registerDto)
        {
            var validationResult = await validatorRegister.ValidateAsync(registerDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<UserDto>.Failure(errors);
            }
            using var transaction = await _employesRepository.BeginTransactionAsync();
            try
            {
                var employe = await _employesRepository.GetByIdAsync(registerDto.EmployeeId);
                if (employe is null)
                    return Result<UserDto>.Failure(new Error(ErrorCodes.NotFound, $"The employee with ID '{registerDto.EmployeeId}' does not exist."));
                if (employe.UserId.HasValue && employe.UserId.Value > 0)
                    return Result<UserDto>.Failure(new Error(ErrorCodes.Conflict, "The employee already has an associated user account."));
                var role = await _roleRepository.GetByIdAsync(registerDto.RoleId);
                if (role is null)
                {
                    return Result<UserDto>.Failure(new Error(ErrorCodes.NotFound, $"The role with ID '{registerDto.RoleId}' does not exist."));
                }
                var emailGenerator = RemoveDiacritics.Remove( $"{employe.FirstName.ToLower()}.{employe.LastName.ToLower()}{employe.Id}@oficentro.com");
                var initialPassword = PasswordGenerator.GenerateTemporaryPassword();

                var userDto = new UserDto
                {
                    Email = emailGenerator,
                    Password = initialPassword,
                };
                var user = _mapper.Map<User>(new UserDto() { Email = emailGenerator });
                var result = await _userManager.CreateAsync(user, initialPassword);

                if (result.Succeeded)
                {
                    var existingUser = await _userManager.FindByEmailAsync(emailGenerator);
                    employe.UserId = existingUser!.Id;
                    await _employesRepository.UpdateEmployeeAsync(employe);
                    await _userManager.AddToRoleAsync(user, role.Name);
                    await _employesRepository.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return Result<UserDto>.Success(userDto);
                }
                else
                {
                    var errors = result.Errors
                        .Select(e => new ValidationError(string.Empty, e.Description))
                        .ToList();
                    return Result<UserDto>.Failure(errors);
                }

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<UserDto>.Failure(new Error(ErrorCodes.Unexpected, ex.Message));
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
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = $"{user.EmployeeUser?.FirstName} {user.EmployeeUser?.LastName}",
                    IsActive = user.IsActive ?? false,
                    Roles = [.. roles]
                }
            };
        }
    }
}
