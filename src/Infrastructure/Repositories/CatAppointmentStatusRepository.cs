using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;


namespace Infrastructure.Repositories;
public class CatAppointmentStatusRepository(ClinicDbContext context) : GenericRepository<CatAppointmentStatus>(context) , ICatAppointmentStatusRepository
{
}
