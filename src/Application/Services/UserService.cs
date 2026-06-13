using Application.DTOs;
using Application.DTOs.User;
using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



//  Implementa IUserService. Se encargará de la lógica
//  relacionada con la gestión de
//  usuarios y sus roles.
namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;

        public UserService ( UserManager<User> userManager, IHttpContextAccessor httpContextAccessor , IUserRepository userRepository)
        {
            this._userManager = userManager;
            this._httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }
       public async Task<Result<PaginatedResponseDto<UserListDto>>> GetAllUsersAsync(PaginationDto pagination)
       {
            try
            {
                System.Linq.Expressions.Expression<System.Func<User, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(pagination.Query))
                {
                    var q = pagination.Query.Trim().ToLower();
                    filter = e => e.Email.ToLower().Contains(q) || 
                                  (e.EmployeeUser != null && (
                                      e.EmployeeUser.FirstName.ToLower().Contains(q) || 
                                      e.EmployeeUser.LastName.ToLower().Contains(q) ||
                                      (e.EmployeeUser.Dni != null && e.EmployeeUser.Dni.ToLower().Contains(q))
                                  ));
                }

                var (baseQuery, total) = await _userRepository.GetQueryAndTotal( 
                    filter: filter,
                    include : q => q.Include(e => e.EmployeeUser).Include(e => e.UserRoleUsers).ThenInclude(ur => ur.Role)
                );
                var proyectQuery = baseQuery
                    .Select(e => new UserListDto
                    {
                        Id = e.Id,
                        EmployerId = e.EmployeeUser != null ? e.EmployeeUser.Id : (int?)null,
                        Email = e.Email,
                        IsActive = e.IsActive,
                        FullName = e.EmployeeUser != null ? $"{e.EmployeeUser.FirstName} {e.EmployeeUser.LastName}" : "Sin Empleado",
                        Dni = e.EmployeeUser != null ? e.EmployeeUser.Dni : null,
                        LastLogin = e.LastLogin,
                        CreatedAt = e.CreatedAt,   
                        Roles  = string.Join(", ", e.UserRoleUsers.Select(ur => ur.Role.Name))
                    });
                var items = await proyectQuery
                       .Skip(pagination.Offset)
                       .Take(pagination.Limit)
                       .ToListAsync();
                var paginatedResponse = new PaginatedResponseDto<UserListDto>(total, items, pagination.Limit);
                return Result<PaginatedResponseDto<UserListDto>>.Success(paginatedResponse);

            }catch( Exception ex )
            {
                return Result<PaginatedResponseDto<UserListDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var email = await GetEmailUserOnlyAsync();
            if (email == null)
            {
                return null;
            }
            return await _userManager.FindByEmailAsync(email);
        }
        public async Task<string?> GetEmailUserOnlyAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }
            var emailClaim = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                return null;
            }
            var email = emailClaim.Value;
            return email;
        } 
    }
}
