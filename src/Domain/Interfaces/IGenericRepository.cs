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

        Task<(List<T> items, int totalItems)> GetTotalAndPagination(int limit, int offset, Expression<Func<T, bool>>? filter = null);
        Task<(IQueryable<T> query, int totalItems)> GetQueryAndTotal(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null);
        Expression<Func<E, bool>> CombineFilters<E>(Expression<Func<E, bool>> f1, Expression<Func<E, bool>> f2);
    }
}
