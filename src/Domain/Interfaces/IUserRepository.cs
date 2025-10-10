using Domain.Entities;

// Define métodos para el acceso a datos del usuario
namespace Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task DeactivateAsync(User user);


        // Métodos para las operaciones de Identity
        Task<User?> FindByEmailAsync(string normalizedEmail);
        Task<User?> FindByIdAsync(string userId);

        // Métodos para lockout
        Task<bool> GetLockoutEnabledAsync(User user);
        Task SetLockoutEnabledAsync(User user, bool enabled);
        Task<int> GetAccessFailedCountAsync(User user);
        Task<int> IncrementAccessFailedCountAsync(User user);
        Task ResetAccessFailedCountAsync(User user);
        Task<DateTimeOffset?> GetLockoutEndDateAsync(User user);
        Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd);

        // metodo para obteener el usuario con con el empleado relacionado
        Task<User?> GetUserWithEmployeeByEmailAsync(string email);

    }
}
