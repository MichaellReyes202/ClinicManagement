using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IEmployesRepository : IGenericRepository<Employee>
    {
        // GetEmployeeWithUserAsync
        Task<Employee?> GetEmployeeWithUserAsync(int id);

        // Actualizar Employee
        Task UpdateEmployeeAsync(Employee employee);
    }
}
