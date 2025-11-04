using Domain.Entities;
using Domain.Errors;
using System.Linq.Expressions;
using System.Threading.Tasks;



namespace Domain.Interfaces
{
    public interface IEmployesRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetEmployeeWithUserAsync(int id);

        // Actualizar Employee
        Task UpdateEmployeeAsync(Employee employee);

    }
}
