namespace Application.DTOs.Prescription;

public class PrescriptionDto
{
    public int Id { get; set; }
    public int ConsultationId { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DoctorName { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}
