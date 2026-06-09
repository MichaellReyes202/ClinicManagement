using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ISpecialtiesRepository : IGenericRepository<Specialty>
    {
        Task<bool> DeleteAsync(int id);
        Task<Specialty?> GetByNameAsync(string name);
    }
}
