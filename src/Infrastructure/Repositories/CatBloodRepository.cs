
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;



namespace Infrastructure.Repositories;

public class CatBloodRepository : GenericRepository<CatBloodType>, ICatBloodRepository
{
    public CatBloodRepository(ClinicDbContext context) : base(context) { }

}
