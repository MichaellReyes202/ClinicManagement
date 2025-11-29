namespace Application.DTOs.Prescription;

public class PrescriptionItemDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = null!;
    public string? Concentration { get; set; }
    public string Dose { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public string Duration { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public string? Instructions { get; set; }
}
