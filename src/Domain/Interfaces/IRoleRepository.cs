using Domain.Entities;

// Define métodos para el acceso a datos de los roles
namespace Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> FindByNameAsync(string roleName);
        Task<IList<string>> GetRolesAsync(User user);
        Task<UserRole?> FindUserRoleAsync(int userId, int roleId);

        // Métodos para las operaciones de Identity
        Task<Role?> FindByIdAsync(string roleId);
        Task<bool> ExistsAsync(string roleName);
        Task AddToRoleAsync(User user, Role role);
        Task RemoveFromRoleAsync(User user, Role role);
        Task<bool> IsInRoleAsync(User user, string roleName);
        Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);

        Task CreateAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Role role);
    }
}
