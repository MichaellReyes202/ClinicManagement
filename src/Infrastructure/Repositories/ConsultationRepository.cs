using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class ConsultationRepository : GenericRepository<Consultation>, IConsultationRepository
{
    public ConsultationRepository(ClinicDbContext context) : base(context)
    {
    }
}
