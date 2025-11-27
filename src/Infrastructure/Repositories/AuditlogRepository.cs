
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class AuditlogRepository(ClinicDbContext context) : GenericRepository<Auditlog>(context) , IAuditlogRepository
{
    public Task UpdateAsync(Auditlog auditlog)
    {
        dbSet.Entry(auditlog).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}