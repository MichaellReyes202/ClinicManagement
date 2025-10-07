using Application.DTOs;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }
        // obtener el role por el id 
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            if (result.IsFailure)
            {
                if (result.Error?.Code == ErrorCodes.NotFound)
                {
                    return NotFound(new { result.Error, result.Error.Description });
                }
            }
            return Ok(result.Value);
        }

        // crear un nuevo role
        [HttpPost("createRole")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto roleDto)
        {
            var result = await _roleService.CreateRoleAsync(roleDto);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetRoleById), new { id = result.Value!.Id }, result.Value);
            }
            if (result.Error!.Code == ErrorCodes.Conflict)
            {
                return Conflict(result.Error); // Retorna 409 Conflict
            }
            return BadRequest(result.Error);
        }

        // asignar un role a un usuario
        [Authorize]
        [HttpPost("assignRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto assignRoleDto)
        {
            var result = await _roleService.AssignRoleToUserAsync(assignRoleDto);
            if (result.IsSuccess)
            {
                return Ok();
            }
            if(result.Error!.Code == ErrorCodes.Unauthorized)
            {
                return Unauthorized(result.Error); // Retorna 401 Unauthorized
            }
            if (result.Error!.Code == ErrorCodes.NotFound)
            {
                return NotFound(result.Error); // Retorna 404 Not Found
            }
            if (result.Error!.Code == ErrorCodes.Conflict)
            {
                return Conflict(result.Error); // Retorna 409 Conflict
            }
            return BadRequest(result.Error); // Retorna 400 Bad Request para otros errores
        }
    }
}
