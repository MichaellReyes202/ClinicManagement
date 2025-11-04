using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;


namespace Infrastructure.Repositories;
public class SexRepository : GenericRepository<CatSexo> , ISexRepository
{
    public SexRepository(ClinicDbContext context) : base(context) {}
}
