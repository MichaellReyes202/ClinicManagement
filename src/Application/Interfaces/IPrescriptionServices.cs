using Application.DTOs.Prescription;
using Domain.Entities;
using Domain.Errors;

namespace Application.Interfaces;

public interface IPrescriptionServices
{
    Task<Result<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionDto dto);
    Task<Result<PrescriptionDto>> GetByConsultationIdAsync(int consultationId);
    Task<Result<IEnumerable<PrescriptionDto>>> GetByPatientIdAsync(int patientId);
}
