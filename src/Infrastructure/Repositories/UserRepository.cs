using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


// Implementa IUserRepository con consultas de
// Entity Framework Core para la entidad User.
namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ClinicDbContext _context;

        public UserRepository(ClinicDbContext context)
        {
            this._context = context;
        }
        public async Task AddAsync(User user)
        {   
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
        public async Task<User?> GetByIdAsync(int id)
        {   
            return await _context.Users.FindAsync(id);
        }

        public async Task DeactivateAsync(int id)
        {
            // Obtener el usuario por su ID
            var user = await _context.Users.FindAsync(id);
            user!.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        { 
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> FindByEmailAsync(string normalizedEmail)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        }

        public async Task<User?> FindByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}
