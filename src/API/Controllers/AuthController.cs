
using Application.DTOs.Auth;
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
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if ( result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    message = result.Error?.Description ?? "One or more validation errors occurred.",
                    errors = result.ValidationErrors
                });
            }
            return result.Error?.Code switch
            {
                ErrorCodes.BadRequest => BadRequest(result.Error),
                ErrorCodes.Conflict => Conflict(result.Error),
                ErrorCodes.NotFound => NotFound(result.Error),
                ErrorCodes.Unexpected => StatusCode(StatusCodes.Status500InternalServerError, result.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unhandled error occurred." })
            };
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponse>> Login(LoginDto login)
        {
            var result = await _authService.LoginAsync(login);
            if(result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if (result.Error?.Code == ErrorCodes.BadRequest || result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    message = result.Error?.Description ?? "One or more validation errors occurred.",
                    errors = result.ValidationErrors
                });
            }
            if(result.Error?.Code == ErrorCodes.Unauthorized)
            {
                return Unauthorized(result.Error);
            }
            if(result.Error?.Code == ErrorCodes.TooManyRequests)
            {
                return StatusCode(429, result.Error);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
        }

        [Authorize]
        [HttpGet("check-status")]
        public async Task<ActionResult<AuthResponse>> CheckAuthStatus()
        {
            var result = await _authService.GetUserOnly();

            if (result.IsSuccess)
                return Ok(result.Value);

            if(result.Error?.Code == ErrorCodes.BadRequest)
            {
                return BadRequest(result.Error);
            }
            if (result.Error?.Code == ErrorCodes.Unauthorized)
            {
                return BadRequest(result.Error);
            }
            return StatusCode(500 , result.Error);

        }

    }
}
