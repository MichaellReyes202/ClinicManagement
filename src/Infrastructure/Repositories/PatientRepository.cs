

using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    public PatientRepository(ClinicDbContext context) : base(context){}

    public Task UpdatePatientAsync(Patient employee)
    {
        dbSet.Entry(employee).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
