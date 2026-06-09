using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ClinicDbContext context) : base(context)
        {
        }

        public override Task UpdateAsync(Appointment appointment)
        {
            _context.Entry(appointment).State = EntityState.Modified;
            return Task.CompletedTask;
        }
    }
} 
