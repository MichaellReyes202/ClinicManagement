using Application.DTOs.Consultation;

namespace Application.Interfaces;

public interface IPdfService
{
    byte[] GenerateConsultationReport(ConsultationDetailDto consultation);
}
