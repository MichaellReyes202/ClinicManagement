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
    public List<ConsultationExamDto> Exams { get; set; } = new();
    public List<ConsultationPrescriptionDto> Prescriptions { get; set; } = new();
}

public class ConsultationExamDto
{
    public int Id { get; set; }
    public string ExamTypeName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Results { get; set; }
}

public class ConsultationPrescriptionDto
{
    public int Id { get; set; }
    public List<ConsultationPrescriptionItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
}

public class ConsultationPrescriptionItemDto
{
    public string MedicationName { get; set; } = null!;
    public string Dose { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public string Duration { get; set; } = null!;
}
