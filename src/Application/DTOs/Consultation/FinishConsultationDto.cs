namespace Application.DTOs.Consultation;

public class FinishConsultationDto
{
    public int ConsultationId { get; set; }
    public string? Reason { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentNotes { get; set; }
}
