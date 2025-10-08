using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
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
    }
}
