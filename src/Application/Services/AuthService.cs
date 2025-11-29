using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Interfaces;
using Application.Util;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
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
using Microsoft.EntityFrameworkCore; // Added for FirstOrDefault extension


namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IValidator<LoginDto> _validatorLogin;
        private readonly IValidator<RegisterDto> validatorRegister;
        private readonly IMapper _mapper;
        private readonly IEmployesRepository _employesRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;
        private readonly IAuditlogServices _auditlogServices;

        public AuthService ( UserManager<User> userManager, SignInManager<User> signInManager, IHttpContextAccessor contextAccessor, IConfiguration configuration, IValidator<LoginDto> validator_login,
            IValidator<RegisterDto> validatorRegister,
            IMapper mapper,
            IEmployesRepository employesRepository,
            IRoleRepository roleRepository,
            IUserRepository userRepository,
            IUserService userService,
            IAuditlogServices auditlogServices

        )
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._configuration = configuration;
            this._validatorLogin = validator_login;
            this.validatorRegister = validatorRegister;
            _mapper = mapper;
            _employesRepository = employesRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _userService = userService;
            _auditlogServices = auditlogServices;
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
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "Invalid Credentials"));

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

            var result = await _signInManager .CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.TooManyRequests, "User account is locked due to multiple failed login attempts. Please try again later."));
            }
            if (result.IsNotAllowed)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "User is not allowed to sign in. Please contact support."));
            }
            if (!result.Succeeded)
            {
                // Audit log para login fallido
                try
                {
                    await _auditlogServices.RegisterActionAsync(
                        userId: user?.Id,
                        module: AuditModuletype.Auth,
                        actionType: ActionType.LOGIN_FAILURE,
                        recordDisplay: loginDto.Email,
                        recordId: user?.Id ?? 0,
                        status: AuditStatus.FAILURE,
                        changeDetail: "Inicio de Sesion Fallida - Credenciales invalidas"
                    );
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine($"Error registrando auditoría: {auditEx.Message}");
                }
                
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "Invalid Credentials"));
            }
            else
            {
                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                // Audit log para login exitoso
                try
                {
                    await _auditlogServices.RegisterActionAsync(
                        userId: user.Id,
                        module: AuditModuletype.Auth,
                        actionType: ActionType.LOGIN_SUCCESS,
                        recordDisplay: user.Email,
                        recordId: user.Id,
                        status: AuditStatus.SUCCESS,
                        changeDetail : "Inicio de sesion exitoso"
                    );
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine($"Error registrando auditoría: {auditEx.Message}");
                }
                
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

                var emailGenerator = RemoveDiacritics.Remove($"{employe.FirstName.ToLower()}.{employe.LastName.ToLower()}{employe.Id}@oficentro.com");
                var initialPassword = PasswordGenerator.GenerateTemporaryPassword();

                var userDto = new UserDto
                {
                    Email = emailGenerator,
                    Password = initialPassword,
                };
                var user = _mapper.Map<User>(new UserDto() { Email = emailGenerator });
                
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    user.CreatedByUserId = currentUser.Id;
                }
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                user.IsActive = true;
                var result = await _userManager.CreateAsync(user, initialPassword);

                if (result.Succeeded)
                {
                    var existingUser = await _userManager.FindByEmailAsync(emailGenerator);
                    employe.UserId = existingUser!.Id;
                    await _employesRepository.UpdateEmployeeAsync(employe);
                    await _userManager.AddToRoleAsync(user, role.Name);
                    await _employesRepository.SaveChangesAsync();

                    // Audit log para registro exitoso
                    try
                    {
                        await _auditlogServices.RegisterActionAsync(
                            userId: existingUser.Id,
                            module: AuditModuletype.Auth,
                            actionType: ActionType.CREATE,
                            recordDisplay: $"{emailGenerator} - {employe.FirstName} {employe.LastName}",
                            recordId: existingUser.Id,
                            status: AuditStatus.SUCCESS,
                            changeDetail: $"User created for employee ID {employe.Id} with role {role.Name}"
                        );
                    }
                    catch (Exception auditEx)
                    {
                        Console.WriteLine($"Error registrando auditoría: {auditEx.Message}");
                    }

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

        public async Task<Result<AuthResponse>> GetUserOnly()
        {
            try
            {
                var email = await _userService.GetEmailUserOnlyAsync();
                if (email == null)
                    return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "User not found (request)"));

                var userOnly = await _userRepository.GetUserWithEmployeeByEmailAsync(email);
                if (userOnly is null)
                    return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "User not Authorize"));
                return Result<AuthResponse>.Success(await GenerateJwtTokenAsync(userOnly));
            }
            catch (Exception)
            {
                return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unexpected, "Internal server Error"));
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

            foreach (var roleName in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
                
                // Buscar el ID del rol para agregarlo como claim
                var roleEntity = (await _roleRepository.GetQuery(r => r.Name == roleName)).FirstOrDefault();
                if (roleEntity != null)
                {
                    claims.Add(new Claim("roleId", roleEntity.Id.ToString()));
                }
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(1);

            var tokenDeSeguridad = new JwtSecurityToken(
                 issuer: null,// jwtSettings["Issuer"],
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
                    IsActive = user.IsActive,
                    Roles = [.. roles],
                    RoleId = (await _roleRepository.GetQuery(r => roles.Contains(r.Name))).FirstOrDefault()?.Id ?? 0, // Agregar RoleId al response si es necesario
                    EmployeeId = user.EmployeeUser?.Id
                }
            };
        }
    }
}
