
using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


//  Contendrá los endpoints para Login y Register
namespace API.Controllers
{
  [ApiController]
  [Route("api/auth")]
  public class AuthController : BaseController
  {
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
      this._authService = authService;
    }

    [HttpPost("register")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterDto register)
    {
      var result = await _authService.RegisterAsync(register);
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), 429)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponse>> Login(LoginDto login)
    {
      var result = await _authService.LoginAsync(login);
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [Authorize]
    [HttpGet("check-status")]
    public async Task<ActionResult<AuthResponse>> CheckAuthStatus()
    {
      var result = await _authService.GetUserOnly();
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("reset-password/{id}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> ResetPassword(int id)
    {
      var result = await _authService.ResetPasswordAsync(id);
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

  }
}
