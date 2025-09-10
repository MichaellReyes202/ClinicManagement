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
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        public async Task<User?> FindByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        

        public async  Task<bool> GetLockoutEnabledAsync(User user)
        {
            return await  Task.FromResult(user.LockoutEnabled.HasValue);
        }

        public async Task SetLockoutEnabledAsync(User user, bool enabled)
        {
            user.LockoutEnabled = enabled;
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetAccessFailedCountAsync(User user)
        {
            return await Task.FromResult(user.AccessFailedCount);
        }

        public async Task<int> IncrementAccessFailedCountAsync(User user)
        {
            user.AccessFailedCount++;
            await _context.SaveChangesAsync();
            return user.AccessFailedCount;
        }

        public async Task ResetAccessFailedCountAsync(User user)
        {
            user.AccessFailedCount = 0;
            await _context.SaveChangesAsync();
        }

        //public async Task<DateTimeOffset?> GetLockoutEndDateAsync(User user)
        //{
        //    return await Task.FromResult<DateTimeOffset?>(user.LockoutEnd == null ? null : new DateTimeOffset(user.LockoutEnd.Value));
        //}

        //public async Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd)
        //{
        //    // Cambiar a UTC y luego a Unspecified para evitar el error.
        //    if (lockoutEnd.HasValue)
        //    {
        //        user.LockoutEnd = DateTime.SpecifyKind(lockoutEnd.Value.UtcDateTime, DateTimeKind.Unspecified);
        //    }
        //    else
        //    {
        //        user.LockoutEnd = null;
        //    }

        //    _context.Entry(user).State = EntityState.Modified;
        //    await _context.SaveChangesAsync();
        //}
        public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user)
        {
            if (user.LockoutEnd.HasValue)
            {
                var result = new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEnd.Value, DateTimeKind.Utc), TimeSpan.Zero);
                return Task.FromResult<DateTimeOffset?>(result);
            }
            return Task.FromResult<DateTimeOffset?>(null);
        }

        public async Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd)
        {
            // Almacena la fecha en UTC para timestamptz
            user.LockoutEnd = lockoutEnd?.UtcDateTime;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

    }
}
