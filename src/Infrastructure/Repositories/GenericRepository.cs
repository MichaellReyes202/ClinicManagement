using Application.DTOs;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class GenericRepository<T> :  IGenericRepository<T> where T : class 
    {
        private readonly ClinicDbContext _context;
        private IDbContextTransaction _currentTransaction;
        internal DbSet<T> dbSet;

        public GenericRepository(ClinicDbContext dbContext)
        {
            _context = dbContext;
            dbSet = _context.Set<T>();
        }

        public async Task<(IQueryable<T> query, int totalItems)> GetQueryAndTotal( Expression<Func<T, bool>>? filter = null,Func<IQueryable<T>, IQueryable<T>>? include = null)
        {
            IQueryable<T> query = dbSet;

            if (include != null)
            {
                query = include(query);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var total = await query.CountAsync(); // total filtrado

            return (query, total);
        }

        //private static IQueryable<EmployeeSearchDto> ApplyOrdering(IQueryable<EmployeeSearchDto> query, string? sortBy,string? sortDirection)
        //{
        //    // Lista blanca de columnas ordenables
        //    var validColumns = new Dictionary<string, Expression<Func<EmployeeSearchDto, object>>>
        //    {
        //        { "FullName", e => e.FullName },
        //        { "Dni", e => e.Dni },
        //        { "Id", e => e.Id }
        //    };

        //    if (string.IsNullOrEmpty(sortBy) || !validColumns.ContainsKey(sortBy))
        //        return query.OrderBy(e => e.FullName); // orden por defecto

        //    var keySelector = validColumns[sortBy];
        //    bool descending = sortDirection?.ToLower() == "desc";

        //    return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
        //}
     
        public async Task<(List<T> items, int totalItems)> GetTotalAndPagination(int limit, int offset, Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            var total = await query.CountAsync();
            var items = await query.Skip(offset).Take(limit).ToListAsync();
            return (items, total);

        }

        public async Task AddAsync(T entity)
        {
            await dbSet.AddAsync(entity);
        }
        public async Task<T> GetByIdAsync(int id)
        {
            return await dbSet.FindAsync(id);
        }
        public async Task<bool> ExistAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.AnyAsync();
        }
        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.ToListAsync();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>>? filter = null, bool tracked = true)
        {
            IQueryable<T> query = dbSet;
            if (!tracked)
            {
                query = query.AsNoTracking();
            }
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.FirstOrDefaultAsync();
        }

        // Métodos de transacción
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
            return _currentTransaction;
        }

        public async Task CommitAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
            }
        }

        public async Task RollbackAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
            }
        }

        // public async Task<(List<T> Datos, int TotalRegistros)> ObtenerPaginadoYTotal(PaginacionDTO paginacion, IQueryable<T> query)

        public async Task<(List<T> items, int totalItems)> GetTotalAndPagination(IQueryable<T> query, int limit, int offset, Expression<Func<T, bool>>? filter = null)
        {
            if (filter != null)
            {
                query = query.Where(filter);
            }
            var total = await query.CountAsync();
            var items = await query.Skip(offset).Take(limit).ToListAsync();
            return (items, total);
        }

        public Expression<Func<E, bool>> CombineFilters<E>(Expression<Func<E, bool>> f1, Expression<Func<E, bool>> f2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var combined = Expression.Lambda<Func<E, bool>>(
                Expression.AndAlso(
                    Expression.Invoke(f1, parameter),
                    Expression.Invoke(f2, parameter)
                ),
                parameter
            );
            return combined;
        }
    }
}
