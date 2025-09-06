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
        public async Task<ActionResult<AuthenticatedUserDto>> Register([FromBody] RegisterDto registerDto)
        {
            // Delegar la lógica de registro al servicio de autenticación
            var response = await _authService.RegisterAsync(registerDto);



            // Retornar la respuesta exitosa
            return Ok(response);
            //try
            //{
               
            //}
            //catch (InvalidOperationException ex)
            //{
            //    // Capturar errores del servicio y devolver una respuesta de error
            //    return BadRequest(new { message = ex.Message });
            //}
            //catch (Exception)
            //{
            //    // Manejar cualquier otro error inesperado
            //    return StatusCode((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            //}
        }
    }
}
