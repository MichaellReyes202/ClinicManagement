using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

// Define métodos para el acceso a datos del usuario
namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task DeactivateAsync(int id);

        // Métodos para las operaciones de Identity
        Task<User?> FindByEmailAsync(string normalizedEmail);
        Task<User?> FindByIdAsync(string userId);

    }
}
