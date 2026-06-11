using Application.DTOs;
using Application.DTOs.Role;
using Application.Interfaces;
using Application.Services;
using Domain.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
  [ApiController]
  [Route("api/roles")]
  public class RoleController : BaseController
  {
    private readonly IRoleService _roleService;
    private readonly ClinicDbContext _context;

    public RoleController(IRoleService roleService, ClinicDbContext context)
    {
      _roleService = roleService;
      _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
      var roles = await _context.Roles
        .Select(r => new { r.Id, r.Name, r.Description })
        .ToListAsync();
      return Ok(roles);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRoleRoot([FromBody] RoleDto roleDto)
    {
      if (string.IsNullOrEmpty(roleDto.Description))
      {
        roleDto.Description = $"Rol {roleDto.Name}";
      }
      var result = await _roleService.CreateRoleAsync(roleDto);
      return result.IsSuccess ? CreatedAtAction(nameof(GetRoleById), new { id = result.Value!.Id }, result.Value) : HandleFailure(result);
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
      return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    // crear un nuevo role
    [HttpPost("createRole")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole([FromBody] RoleDto roleDto)
    {
      var result = await _roleService.CreateRoleAsync(roleDto);
      return result.IsSuccess ? CreatedAtAction(nameof(GetRoleById), new { id = result.Value!.Id }, result.Value) : HandleFailure(result);
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
      return result.IsSuccess ? Ok() : HandleFailure(result);
    }
  }
}
