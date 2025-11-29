using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PrescriptionRepository : GenericRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ClinicDbContext context) : base(context)
    {
    }

    public async Task<Prescription?> GetByConsultationIdAsync(int consultationId)
    {
        return await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .ThenInclude(pi => pi.Medication)
            .Include(p => p.Consultation)
            .ThenInclude(c => c.Employee)
            .FirstOrDefaultAsync(p => p.ConsultationId == consultationId);
    }

    public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId)
    {
        return await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .ThenInclude(pi => pi.Medication)
            .Include(p => p.Consultation)
            .Where(p => p.Consultation != null && p.Consultation.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
