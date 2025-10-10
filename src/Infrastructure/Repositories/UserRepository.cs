using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


// Implementa IUserRepository con consultas de
// Entity Framework Core para la entidad User.
namespace Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User> , IUserRepository
    {
        private readonly ClinicDbContext _context;

        public UserRepository(ClinicDbContext context) : base(context)
        {
            _context = context;
        }
        public Task DeactivateAsync(User user)
        {
            user!.IsActive = false;
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChangesAsync();
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public Task UpdateAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public async Task<User?> FindByEmailAsync(string normalizedEmail)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        public async Task<User?> FindByIdAsync(string userId)
        {
            if (int.TryParse(userId, out int id))
            {
                return await GetByIdAsync(id);
            }
            return null;
        }
        public async Task<bool> GetLockoutEnabledAsync(User user)
        {
            return await Task.FromResult(user.LockoutEnabled.HasValue);
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

        public Task<User?> GetUserWithEmployeeByEmailAsync(string email )
        {
            var emailNormalized = email.ToUpper();
            return _context.Users
                .Include(u => u.EmployeeUser)
                .FirstOrDefaultAsync(u => u.NormalizedEmail == emailNormalized);
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

    }
}
