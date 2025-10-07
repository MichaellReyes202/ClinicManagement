using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Domain.Interfaces
{
    public interface IGenericRepository<T> where T : class 
    {
        Task AddAsync(T entity);
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
        Task<T?> GetAsync(Expression<Func<T, bool>>? filter = null, bool tracked = true);
        Task<T> GetByIdAsync(int id);
        Task<bool> ExistAsync(Expression<Func<T, bool>>? filter = null);

        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();

        Task CommitAsync();
        Task RollbackAsync();



    }
}
