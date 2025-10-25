using Application.DTOs;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IRoleService
    {
        Task<Result> UpdateRoleAsync(int idRole, RoleDto roleDto);
        string GetRoleName(string roleName);
        // obtener el role por el id
        Task<Result<Role>> GetRoleByIdAsync(int id);
        Task<bool> RoleExistsAsync(string roleName);
        Task<Result> AssignRoleToUserAsync(AssignRoleDto assignRoleDto);
        Task RemoveRoleFromUserAsync(string email, string roleName);
        Task<IList<string>> GetUserRolesAsync(string email);
        Task<IEnumerable<string>> GetUsersInRoleAsync(string roleName);
        Task<bool> IsUserInRoleAsync(string email, string roleName);
        Task<Result<Role>> CreateRoleAsync(RoleDto roleCreaction);
        Task DeleteRoleAsync(string roleName);
        Task<List<OptionDto>> GetAllRolesOptions();
    }
}
