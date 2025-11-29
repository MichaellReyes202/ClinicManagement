using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class MedicationRepository : GenericRepository<Medication>, IMedicationRepository
{
    public MedicationRepository(ClinicDbContext context) : base(context)
    {
    }
}
