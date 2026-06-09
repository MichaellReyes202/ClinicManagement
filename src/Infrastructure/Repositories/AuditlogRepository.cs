
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AuditlogRepository : GenericRepository<Auditlog>, IAuditlogRepository
    {
        public AuditlogRepository(ClinicDbContext context) : base(context)
        {
        }

        public override Task UpdateAsync(Auditlog auditlog)
        {
            _context.Entry(auditlog).State = EntityState.Modified;
            return Task.CompletedTask;
        }
    }
} 