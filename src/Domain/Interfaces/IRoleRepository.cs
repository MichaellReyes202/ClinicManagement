using Domain.Entities;

// Define métodos para el acceso a datos de los roles
namespace Domain.Interfaces
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> FindByNameAsync(string roleName);
        Task<IList<string>> GetRolesAsync(User user);
        Task<UserRole?> FindUserRoleAsync(int userId, int roleId);


        // Métodos para las operaciones de Identity
        Task<Role?> FindByIdAsync(string roleId);
        Task AddToRoleAsync(User user, Role role  , int userIdOnly);
        Task RemoveFromRoleAsync(User user, Role role);
        Task<bool> IsInRoleAsync(User user, string roleName);
        Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);


        Task UpdateAsync(Role role);
        Task DeleteAsync(Role role);

        

    }
}
