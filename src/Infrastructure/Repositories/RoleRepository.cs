using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<Role> ,  IRoleRepository
    {
        private readonly ClinicDbContext _context;

        public RoleRepository(ClinicDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<Role?> FindByNameAsync(string roleName)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
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
                return await GetByIdAsync(id);
            }
            return null;
        }
        

        public Task AddToRoleAsync(User user, Role role , int userIdOnly)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id ,
                CreatedByUserId = userIdOnly,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
            return Task.CompletedTask;
        }

        public async Task RemoveFromRoleAsync(User user, Role role)
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id);
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
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



        public async Task UpdateAsync(Role role)
        {
            _context.Entry(role).State = EntityState.Modified;
        }

        public async Task DeleteAsync(Role role)
        {
            _context.Roles.Remove(role);
        }
    }
}
