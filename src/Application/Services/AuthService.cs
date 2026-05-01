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
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.EntityFrameworkCore; // Added for FirstOrDefault extension
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Application.Services;


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
  private readonly IEmailService _emailService;
  private readonly IMemoryCache _memoryCache;

  public AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IConfiguration configuration,
    IValidator<LoginDto> validator_login,
    IValidator<RegisterDto> validatorRegister,
    IMapper mapper,
    IEmployesRepository employesRepository,
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IUserService userService,
    IAuditlogServices auditlogServices,
    IEmailService emailService,
    IMemoryCache memoryCache

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
    _emailService = emailService;
    _memoryCache = memoryCache;
  }

  public async Task<Result<AuthResponse>> LoginAsync(LoginDto loginDto)
  {
    var validationResult = await _validatorLogin.ValidateAsync(loginDto);

    if (!validationResult.IsValid)
    {
      var errors = validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)).ToList();
      return Result<AuthResponse>.Failure(errors);
    }
    var user = await _userRepository.GetUserWithEmployeeByEmailAsync(loginDto.Email);
    if (user is null)
    {
      return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "Invalid Credentials"));
    }

    // Verificamos si el usuario esta bloqueado de antemano 
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
        var metadata = new Dictionary<string, object>();
        var remainingTime = lockoutEnd.Value - DateTimeOffset.UtcNow; // calculos de los minutos restantes 
        var remainingMinutes = (int)Math.Ceiling(remainingTime.TotalMinutes);

        // Metada
        metadata.Add("isLockedOut", true);
        metadata.Add("minutesToWait", remainingMinutes > 0 ? remainingMinutes : 1);
        metadata.Add("lockoutEndUtc", lockoutEnd.Value.UtcDateTime);

        return Result<AuthResponse>.Failure(new Error(
            ErrorCodes.TooManyRequests,
            "Account locked due to multiple failed login attempts.",
            null,
            metadata
        ));
      }
    }

    var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

    if (result.IsLockedOut)
    {
      var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
      var metadata = new Dictionary<string, object>();
      var remainingTime = lockoutEnd.Value - DateTimeOffset.UtcNow;
      var remainingMinutes = (int)Math.Ceiling(remainingTime.TotalMinutes);

      // Metada
      metadata.Add("isLockedOut", true);
      metadata.Add("minutesToWait", remainingMinutes > 0 ? remainingMinutes : 1);
      metadata.Add("lockoutEndUtc", lockoutEnd.Value.UtcDateTime);

      return Result<AuthResponse>.Failure(new Error(
          ErrorCodes.TooManyRequests,
          "Account locked due to multiple failed login attempts.",
          null,
          metadata
      ));

    }
    if (result.IsNotAllowed)
    {
      return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "User is not allowed to sign in. Please contact support."));
    }

    if (!result.Succeeded)
    {
      return Result<AuthResponse>.Failure(new Error(ErrorCodes.Unauthorized, "Invalid Credentials"));
    }


    await _auditlogServices.RegisterLoginActionAsync(succes: result.Succeeded, user.Id, recordDisplay: user.Email);
    user.LastLogin = DateTime.UtcNow;
    await _userRepository.UpdateAsync(user);
    await _userRepository.SaveChangesAsync();

    return Result<AuthResponse>.Success(await GenerateJwtTokenAsync(user));



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
        var errors = result.Errors.Select(e => new ValidationError(string.Empty, e.Description)).ToList();
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
        EmployeeId = user.EmployeeUser?.Id,
        RequiresPasswordChange = user.RequiresPasswordChange
      }
    };
  }
  public async Task<Result<UserDto>> ResetPasswordAsync(int userId)
  {
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null)
      return Result<UserDto>.Failure(new Error(ErrorCodes.NotFound, "User not found"));

    var employe = await _employesRepository.GetByIdAsync(user.EmployeeUser?.Id ?? 0);
    if (employe == null || string.IsNullOrEmpty(employe.Email))
    {
      return Result<UserDto>.Failure(new Error(ErrorCodes.Conflict, "El empleado no tiene un correo personal registrado. Registre un correo personal para restablecer la contraseña."));
    }

    var newPassword = PasswordGenerator.GenerateTemporaryPassword();
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

    if (result.Succeeded)
    {
      user.RequiresPasswordChange = true;
      await _userRepository.UpdateAsync(user);
      await _userRepository.SaveChangesAsync();

      // Audit log for password reset
      try
      {
        await _auditlogServices.RegisterActionAsync(
            userId: user.Id,
            module: AuditModuletype.Auth,
            actionType: ActionType.UPDATE,
            recordDisplay: user.Email,
            recordId: user.Id,
            status: AuditStatus.SUCCESS,
            changeDetail: "Password reset by admin"
        );
      }
      catch (Exception auditEx)
      {
        Console.WriteLine($"Error registering audit: {auditEx.Message}");
      }

      // Send email with new password
      var subject = "Restablecimiento de contraseña - Clínica Oficentro";
      var body = $"<p>Hola {employe.FirstName},</p><p>El administrador ha restablecido tu contraseña.</p><p>Tu nueva contraseña temporal es: <strong>{newPassword}</strong></p><p>Por favor inicia sesión con esta contraseña. Se te pedirá cambiarla inmediatamente.</p>";
      
      await _emailService.SendEmailAsync(employe.Email, subject, body);

      return Result<UserDto>.Success(new UserDto
      {
        Email = employe.Email, // Retornamos el correo al que se envio para info
        Password = "" // Ya no retornamos la contraseña, solo indicamos éxito
      });
    }

    return Result<UserDto>.Failure(new Error(ErrorCodes.Unexpected, "Failed to reset password"));
  }

  public async Task<Result<bool>> ChangePasswordAsync(ChangePasswordDto dto)
  {
    var user = await _userService.GetCurrentUserAsync();
    if (user == null)
      return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "User not found"));

    var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

    if (result.Succeeded)
    {
      user.RequiresPasswordChange = false;
      await _userRepository.UpdateAsync(user);
      await _userRepository.SaveChangesAsync();

      try
      {
        await _auditlogServices.RegisterActionAsync(
            userId: user.Id,
            module: AuditModuletype.Auth,
            actionType: ActionType.UPDATE,
            recordDisplay: user.Email,
            recordId: user.Id,
            status: AuditStatus.SUCCESS,
            changeDetail: "User forced password change completed"
        );
      }
      catch (Exception auditEx)
      {
        Console.WriteLine($"Error registering audit: {auditEx.Message}");
      }

      return Result<bool>.Success(true);
    }

    var errors = result.Errors.Select(e => new ValidationError(string.Empty, e.Description)).ToList();
    return Result<bool>.Failure(errors);
  }

  public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto dto)
  {
    var user = await _userRepository.GetUserWithEmployeeByEmailAsync(dto.Email);
    if (user == null)
    {
      // Log de intento fallido (usuario inexistente) para detectar escaneo de correos
        await _auditlogServices.RegisterActionAsync(
          userId: null,
          module: AuditModuletype.Auth,
          actionType: ActionType.UPDATE, // O RESET_PASSWORD si lo tienes en el enum
          recordDisplay: dto.Email,
          recordId: 0,
          status: AuditStatus.FAILURE,
          changeDetail: "Intento de recuperación de contraseña para correo no registrado."
        );
      // Por seguridad no indicamos que el usuario no existe, solo simulamos éxito
      return Result<bool>.Success(true); 
    }

    if (user.EmployeeUser == null || string.IsNullOrEmpty(user.EmployeeUser.Email))
    {
      return Result<bool>.Failure(new Error(ErrorCodes.Conflict, "No hay un correo personal registrado. Contacte al administrador."));
    }

    var random = new Random();
    var code = random.Next(100000, 999999).ToString();

    // Guardar en cache por 5 minutos
    _memoryCache.Set($"pwd_reset_{user.Email}", code, TimeSpan.FromMinutes(5));

    try{
      var subject = "Código de recuperación de contraseña - Clínica Oficentro";
    var resetLink = $"{_configuration["FrontendUrl"] ?? "http://localhost:5173"}/auth/reset-password?email={user.Email}";
    var body = $@"
        <p>Hola {user.EmployeeUser.FirstName},</p>
        <p>Has solicitado recuperar tu contraseña.</p>
        <p>Tu código de recuperación es: <strong>{code}</strong></p>
        <p>Este código es válido por 5 minutos.</p>
        <p>Puedes cambiar tu contraseña en el siguiente enlace: <a href='{resetLink}'>Restablecer contraseña</a></p>
    ";

    await _emailService.SendEmailAsync(user.EmployeeUser.Email, subject, body);

    // Log de éxito en envío de código
    await _auditlogServices.RegisterActionAsync(
        userId: user.Id,
        module: AuditModuletype.Auth,
        actionType: ActionType.UPDATE,
        recordDisplay: user.Email,
        recordId: user.Id,
            status: AuditStatus.SUCCESS,
            changeDetail: "Código de recuperación enviado al correo personal."
        );

    return Result<bool>.Success(true);
    } catch(Exception ex){
      // Log de error técnico en el envío
      await _auditlogServices.RegisterActionAsync(
        userId: user.Id,
        module: AuditModuletype.Auth,
        actionType: ActionType.UPDATE,
        recordDisplay: user.Email,
        recordId: user.Id,
        status: AuditStatus.FAILURE,
        changeDetail: $"Error al enviar correo de recuperación: {ex.Message}"
      );
      throw;
    }
  }

  public async Task<Result<bool>> ResetPasswordWithCodeAsync(ResetPasswordWithCodeDto dto)
  {
    var user = await _userRepository.GetUserWithEmployeeByEmailAsync(dto.Email);
    if (user == null)
      return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "User not found"));

    if (!_memoryCache.TryGetValue($"pwd_reset_{user.Email}", out string? savedCode) || savedCode != dto.Code)
    {
      await _auditlogServices.RegisterActionAsync(
        userId: user.Id,
        module: AuditModuletype.Auth,
        actionType: ActionType.UPDATE,
        recordDisplay: user.Email,
        recordId: user.Id,
        status: AuditStatus.FAILURE,
        changeDetail: "Código de recuperación inválido o expirado."
      );
      return Result<bool>.Failure(new Error(ErrorCodes.Unauthorized, "El código es inválido o ha expirado."));
    }

    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

    if (result.Succeeded)
    {
      _memoryCache.Remove($"pwd_reset_{user.Email}");
      user.RequiresPasswordChange = false;
      await _userRepository.UpdateAsync(user);
      await _userRepository.SaveChangesAsync();

      // Log de éxito total
      await _auditlogServices.RegisterActionAsync(
        userId: user.Id,
        module: AuditModuletype.Auth,
        actionType: ActionType.RESET_PASSWORD,
        recordDisplay: user.Email,
        recordId: user.Id,
        status: AuditStatus.SUCCESS,
        changeDetail: "Contraseña restablecida exitosamente mediante código."
      );

      return Result<bool>.Success(true);
    }

    var errors = result.Errors.Select(e => new ValidationError(string.Empty, e.Description)).ToList();
    return Result<bool>.Failure(errors);
  }
}