using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;


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
        public async Task<ActionResult<AuthResponse>> Register( RegisterDto register)
        {
            var result = await _authService.RegisterAsync(register);
            if(result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if (result.ValidationErrors.Any())
            {
                // Agrupa los errores por el nombre de la propiedad
                var errors = result.ValidationErrors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                // Devuelve una respuesta estructurada similar a ModelState
                return BadRequest(new { errors = errors });
            }
            else
            {
                // Esto maneja errores de negocio (ej. "Credenciales inválidas")
                return BadRequest(new { 
                    code = result.ErrorCode,
                    error = result.Error 
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login( LoginDto login)
        {
            var result = await _authService.LoginAsync(login);

            if(result.IsSuccess)
            {
                return Ok(result.Value);
            }
            if(result.ValidationErrors.Count != 0)
            {
                var validationErrors = result.ValidationErrors.ToDictionary(
                    e => e.PropertyName,
                    e => new[] { e.ErrorMessage }
                );
                return BadRequest(new { errors = validationErrors });
            }
            else
            {
                // Esto maneja errores de negocio (ej. "Credenciales inválidas")
                return BadRequest(new
                {
                    code = result.ErrorCode,
                    error = result.Error
                });
            }
        }
    }
}
