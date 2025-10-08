using Application.DTOs;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


//  Contendrá los endpoints para Login y Register
namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            this._authService = authService;
        }

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<AuthResponse>> Register(RegisterDto register)
        {
            var result = await _authService.RegisterAsync(register);
            if (result.IsFailure)
            {
                if (result.ValidationErrors.Count != 0)
                {
                    return BadRequest(result.ValidationErrors);
                }
                if (result.Error?.Code == ErrorCodes.BadRequest)
                {
                    return NotFound(new { result.Error, result.Error.Description });
                }
                if (result.Error?.Code == ErrorCodes.TooManyRequests)
                {
                    return StatusCode(429,new { result.Error, result.Error.Description });
                }
                if (result.Error?.Code == ErrorCodes.Conflict)
                {
                    return Conflict(new { result.Error, result.Error.Description });
                }
            }
            return Ok(result.Value);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponse>> Login(LoginDto login)
        {
            var result = await _authService.LoginAsync(login);
            if (result.IsFailure)
            {
                if (result.ValidationErrors.Count != 0)
                {
                    return BadRequest(result.ValidationErrors);
                }
                if (result.Error?.Code == ErrorCodes.Unauthorized)
                {
                    return Unauthorized(new { result.Error, result.Error.Description });
                }
                return StatusCode(500, new { result.Error, result.Error!.Description });
            }
            return Ok(result.Value);
        }
    }
}
