using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SpecialtiesRepository : GenericRepository<Specialty> , ISpecialtiesRepository
    {
        public SpecialtiesRepository(ClinicDbContext context) : base(context)
        {
        }
        public async Task<Specialty?> GetByNameAsync(string name)
        {
            return await _context.Specialties.FirstOrDefaultAsync(s => s.Name == name);
        }

        public override async Task UpdateAsync(Specialty specialty)
        {
            _context.Entry(specialty).State = EntityState.Modified;
            await SaveChangesAsync();
        }
        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

    }
}
