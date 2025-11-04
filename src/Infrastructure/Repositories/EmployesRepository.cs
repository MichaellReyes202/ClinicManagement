
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories
{
    public class EmployesRepository: GenericRepository<Employee> , IEmployesRepository
    {
        public EmployesRepository( ClinicDbContext context) : base(context)
        {}
        public Task<Employee?> GetEmployeeWithUserAsync(int id)
        {
            return dbSet.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        }
        public Task UpdateEmployeeAsync(Employee employee)
        {
            dbSet.Entry(employee).State = EntityState.Modified;
            return Task.CompletedTask;
        }

       
    } 
}
