using Application.DTOs.Consultation;
using Domain.Errors;

namespace Application.Interfaces;

public interface IConsultationServices
{
    Task<Result<int>> StartConsultationAsync(int appointmentId);
    Task<Result> FinishConsultationAsync(FinishConsultationDto dto);
    Task<Result> RollbackConsultationAsync(int consultationId);
    Task<Result<ConsultationDetailDto>> GetConsultationByAppointmentIdAsync(int appointmentId);
    Task<Result<List<ConsultationDetailDto>>> GetConsultationsByPatientIdAsync(int patientId);
    Task<Result<List<ConsultationDetailDto>>> GetAllConsultationsAsync();
}
