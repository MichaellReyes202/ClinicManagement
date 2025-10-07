using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class EmployesRepository(ClinicDbContext context) : GenericRepository<Employee>(context) , IEmployesRepository
    {
    }
}
