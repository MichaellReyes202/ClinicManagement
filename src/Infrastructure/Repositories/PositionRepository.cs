using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PositionRepository : GenericRepository<Position> , IPositionRepository
    {
        public PositionRepository( ClinicDbContext context) : base(context)
        {
        }

        public override async Task UpdateAsync(Position position)
        {
            _context.Entry(position).State = EntityState.Modified;
            await SaveChangesAsync();
        }
    }
}
