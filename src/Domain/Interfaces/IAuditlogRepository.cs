using Domain.Entities;
namespace Domain.Interfaces;
public interface IAuditlogRepository : IGenericRepository<Auditlog>
{
    Task UpdateAsync(Auditlog auditlog);
}