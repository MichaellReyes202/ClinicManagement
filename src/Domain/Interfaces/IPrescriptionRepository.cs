using Domain.Entities;

namespace Domain.Interfaces;

public interface IPrescriptionRepository : IGenericRepository<Prescription>
{
    Task<Prescription?> GetByConsultationIdAsync(int consultationId);
    Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId);
}
