// Define la interfaz para operaciones sobre usuarios
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserService
    {
        // metodod para obtener el usuario que esta logueado
        Task<User?> GetCurrentUserAsync();

        // obtener todos los usuarios
        Task<IEnumerable<User>> GetAllUsersAsync();
    }
}
