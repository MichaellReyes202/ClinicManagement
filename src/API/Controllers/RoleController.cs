using Application.DTOs;
using Application.DTOs.Role;
using Application.Interfaces;
using Application.Services;
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

        [HttpGet("listOption")]
        public async Task<ActionResult<List<OptionDto>>> GetListOption()
        {
            return await _roleService.GetAllRolesOptions();
        }


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
            if (result.Error?.Code == ErrorCodes.BadRequest || result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    message = result.Error?.Description ?? "One or more validation errors occurred.",
                    errors = result.ValidationErrors
                });
            }
            if (result.Error!.Code == ErrorCodes.Conflict)
            {
                return Conflict(result.Error); // Retorna 409 Conflict
            }
            return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
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
            if (result.Error?.Code == ErrorCodes.BadRequest || result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    message = result.Error?.Description ?? "One or more validation errors occurred.",
                    errors = result.ValidationErrors
                });
            }
            if (result.Error!.Code == ErrorCodes.Unauthorized)
            {
                return Unauthorized(result.Error);
            }
            if (result.Error!.Code == ErrorCodes.NotFound)
            {
                return NotFound(result.Error);
            }
            if (result.Error!.Code == ErrorCodes.Conflict)
            {
                return Conflict(result.Error);
            }
            return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
        }
    }
}
