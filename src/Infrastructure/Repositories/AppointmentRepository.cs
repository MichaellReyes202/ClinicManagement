using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AppointmentRepository(ClinicDbContext context) : GenericRepository<Appointment>(context), IAppointmentRepository
{
    public Task UpdateAsync(Appointment appointment)
    {
        dbSet.Entry(appointment).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
