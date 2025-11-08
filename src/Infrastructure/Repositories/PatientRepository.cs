

using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class PatientRepository(ClinicDbContext context) : GenericRepository<Patient>(context), IPatientRepository
{
    public Task UpdatePatientAsync(Patient employee)
    {
        dbSet.Entry(employee).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
