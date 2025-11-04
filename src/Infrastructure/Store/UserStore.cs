
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Store
{
    public class UserStore :
        IUserStore<User>,
        IUserPasswordStore<User>,
        IUserEmailStore<User>,
        IUserRoleStore<User>,

       IUserLockoutStore<User>

    {
        private readonly IUserRepository _userRepository;
        private readonly ILookupNormalizer _normalizer;
        private readonly IRoleRepository _roleRepository;

        public UserStore
        (
            IUserRepository userRepository,
            ILookupNormalizer normalizer,
            IRoleRepository roleRepository 
        )
        {
            this._userRepository = userRepository;
            this._normalizer = normalizer;
            this._roleRepository = roleRepository;
        }
        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return IdentityResult.Success;
        }
        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _userRepository.DeleteAsync(user);
            await _userRepository.SaveChangesAsync();
            return IdentityResult.Success;
        }
        public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (int.TryParse(userId, out int id))
            {
                return await _userRepository.FindByIdAsync(id.ToString());
            }
            return null;
        }
        public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _userRepository.FindByEmailAsync(normalizedUserName);
        }
        public async Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var email = await _userRepository.FindByIdAsync(user.Id.ToString());
            return email?.NormalizedEmail.ToUpper();
        }
        public async Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await Task.FromResult(user.Id.ToString());
        }

        public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException("The username cannot be null or empty.", nameof(userName));
            }

            user.Email = userName;
            return Task.CompletedTask;
        }
        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            return IdentityResult.Success;
        }
        public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            // Asigna el nombre de usuario. Para tu caso, el email es el nombre de usuario.
            return Task.FromResult(user.Email);
        }



        public void Dispose()
        {
            // Los repositorios se gestionan por el contenedor de inyección de dependencias de .NET Core,
            // que se encarga de su ciclo de vida. No es necesario liberar recursos aquí manualmente.
        }


        public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _userRepository.FindByEmailAsync(normalizedEmail);
        }
        public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.Email);
        }
        public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();

        }
        public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Retorna el email normalizado. Si el email es nulo, retorna nulo.
            return Task.FromResult(_normalizer.NormalizeEmail(user.Email));
        }
        public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Asigna el email a la propiedad Email de la entidad
            user.Email = email;

            // Actualiza también el NormalizedEmail para futuras búsquedas.
            // Esto es crucial para un rendimiento óptimo.
            // Necesitas inyectar ILookupNormalizer en el constructor de tu UserStore.
            user.NormalizedEmail = string.IsNullOrEmpty(email) ? null : _normalizer.NormalizeEmail(email);

            return Task.CompletedTask;
        }
        public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            // email
            throw new NotImplementedException();
        }
        public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Asigna el valor normalizado directamente a la propiedad de la entidad.
            user.NormalizedEmail = normalizedEmail;

            return Task.CompletedTask;
        }


        /// ---------------------------------------------------------------------
        public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Retorna el hash de la contraseña de la entidad.
            return Task.FromResult(user.PasswordHash);
        }
        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Retorna true si la propiedad PasswordHash no es nula o una cadena vacía.
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        }
        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.NormalizedEmail = normalizedName;

            return Task.CompletedTask;
        }
        public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Asigna el hash de la contraseña a la propiedad de la entidad.
            user.PasswordHash = passwordHash;

            return Task.CompletedTask;
        }




        // ----------------- Roles -----------------
        public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(roleName));

            var role = await _roleRepository.FindByNameAsync(roleName);
            if (role == null)
            {
                throw new InvalidOperationException($"Role {roleName} not found.");
            }
            var userOnly = await _userRepository.FindByIdAsync(user.Id.ToString()) ?? throw new InvalidOperationException($"User with ID {user.Id} not found.");
            await _roleRepository.AddToRoleAsync(user, role , userOnly.Id);
            await _userRepository.SaveChangesAsync();

        }

        public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(roleName));

            var role = await _roleRepository.FindByNameAsync(roleName);
            if (role == null) return; // Si el rol no existe, no hay nada que remover.

            await _roleRepository.RemoveFromRoleAsync(user, role);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null) throw new ArgumentNullException(nameof(user));

            return await _roleRepository.GetRolesAsync(user);
        }

        public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(roleName));

            return await _roleRepository.IsInRoleAsync(user, roleName);
        }

        public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(roleName));

            var users = await _roleRepository.GetUsersInRoleAsync(roleName);

            return users.ToList();
        }



        // ----------------- Lockout -----------------

        public async Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _userRepository.GetAccessFailedCountAsync(user);
        }

        public async Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
        {
            return await _userRepository.GetLockoutEnabledAsync(user);
        }

        public async Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
        {
            return await _userRepository.GetLockoutEndDateAsync(user);
        }

        public async Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            return await _userRepository.IncrementAccessFailedCountAsync(user);
        }


        public async Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            await _userRepository.ResetAccessFailedCountAsync(user);
        }

        public async Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        {
            await _userRepository.SetLockoutEnabledAsync(user, enabled);
        }

        public async Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            await _userRepository.SetLockoutEndDateAsync(user, lockoutEnd);
        }
    }
}
