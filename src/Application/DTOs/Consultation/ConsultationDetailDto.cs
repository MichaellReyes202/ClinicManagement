using System;

namespace Application.DTOs.Consultation;

public class ConsultationDetailDto
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = null!;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = null!;
    public string? Reason { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentNotes { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
