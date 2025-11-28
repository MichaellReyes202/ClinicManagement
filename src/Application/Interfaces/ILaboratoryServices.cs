using Application.DTOs.Laboratory;
using Domain.Errors;

namespace Application.Interfaces;

public interface ILaboratoryServices
{
    Task<Result> CreateExamOrderAsync(ExamOrderDto dto);
    Task<Result> ProcessExamAsync(ExamProcessDto dto);
    Task<Result<List<ExamPendingDto>>> GetPendingExamsAsync();
    Task<Result<List<ExamPendingDto>>> GetExamsByAppointmentIdAsync(int appointmentId);
    Task<Result<List<ExamPendingDto>>> GetExamsByPatientIdAsync(int patientId);
}
