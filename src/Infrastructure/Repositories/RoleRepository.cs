using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ClinicDbContext _context;

        public RoleRepository(ClinicDbContext context)
        {
            this._context = context;
        }
        public async Task<Role?> FindByNameAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        }


        public async Task<IList<string>> GetRolesAsync(User user)
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role) // Asegúrate de cargar la entidad Role relacionada
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return roles;
        }

        public async Task<UserRole?> FindUserRoleAsync(int userId, int roleId)
        {
            return await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        public async Task<Role?> FindByIdAsync(string roleId)
        {
            if (int.TryParse(roleId, out int id))
            {
                return await _context.Roles.FindAsync(id);
            }
            return null;
        }

        public async Task<bool> ExistsAsync(string roleName)
        {
            return await _context.Roles.AnyAsync(r => r.Name == roleName);
        }

        public async Task AddToRoleAsync(User user, Role role)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromRoleAsync(User user, Role role)
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id);
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName)
        {
            var role = await FindByNameAsync(roleName);
            if (role == null) return false;
            var userRole = await FindUserRoleAsync(user.Id, role.Id);
            return userRole != null;
        }

        public async Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName)
        {
            var role = await FindByNameAsync(roleName);
            if (role == null) return Enumerable.Empty<User>();
            var users = await _context.UserRoles
                .Where(ur => ur.RoleId == role.Id)
                .Include(ur => ur.User) // Asegúrate de cargar la entidad User relacionada
                .Select(ur => ur.User)
                .ToListAsync();
            return users;
        }

        public async Task CreateAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Role role)
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
    }
}
