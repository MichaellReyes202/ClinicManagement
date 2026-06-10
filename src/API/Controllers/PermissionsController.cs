using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Caching.Memory;

namespace API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "RequireAdministrationView")]
public class PermissionsController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionsController(ClinicDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        var views = await _context.CatViews
            .Select(v => new { v.Id, v.Name, v.Route, v.Description })
            .ToListAsync();
        return Ok(views);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserPermissions(int userId)
    {
        var userViewIds = await _context.UserViews
            .Where(uv => uv.UserId == userId)
            .Select(uv => uv.ViewId)
            .ToListAsync();
        return Ok(userViewIds);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsDto dto)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        int? adminId = null;
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(email))
        {
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (adminUser != null)
            {
                adminId = adminUser.Id;
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.UserViews.Where(uv => uv.UserId == dto.UserId).ToListAsync();
            _context.UserViews.RemoveRange(existing);
            await _context.SaveChangesAsync();

            if (dto.ViewIds != null && dto.ViewIds.Any())
            {
                var newViews = dto.ViewIds.Select(viewId => new UserView
                {
                    UserId = dto.UserId,
                    ViewId = viewId,
                    GrantedByUserId = adminId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.UserViews.AddRangeAsync(newViews);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Guardar la fecha de actualización de permisos en caché para invalidar el token actual del usuario
            var cacheKey = $"user-permissions-updated:{dto.UserId}";
            _cache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromDays(1));

            return Ok(new { message = "Permissions updated successfully" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "An error occurred while updating permissions", error = ex.Message });
        }
    }
}

public class AssignPermissionsDto
{
    public int UserId { get; set; }
    public List<int> ViewIds { get; set; } = new();
}
