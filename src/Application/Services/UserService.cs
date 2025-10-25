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
                var (baseQuery, total) = await _userRepository.GetQueryAndTotal( 
                    include : q => q.Include(e => e.EmployeeUser).Include(e => e.UserRoleUsers).ThenInclude(ur => ur.Role)
                );
                var proyectQuery = baseQuery
                    .Select(e => new UserListDto
                    {
                        Id = e.Id,
                        EmployerId = e.EmployeeUser.Id,
                        Email = e.Email,
                        IsActive = e.IsActive,
                        FullName = $"{e.EmployeeUser!.FirstName } {e.EmployeeUser.LastName}",
                        Dni = e.EmployeeUser.Dni,
                        LastLogin = e.LastLogin,
                        CreatedAt = e.CreatedAt,   
                        Roles  = string.Join(", ", e.UserRoleUsers.Select(ur => ur.Role.Name))
                    });
                var items = await proyectQuery
                       .Skip(pagination.Offset)
                       .Take(pagination.Limit)
                       .ToListAsync();
                var paginatedResponse = new PaginatedResponseDto<UserListDto>(total, items);
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
