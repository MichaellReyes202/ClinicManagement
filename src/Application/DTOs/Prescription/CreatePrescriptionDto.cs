namespace Application.DTOs.Prescription;

public class CreatePrescriptionItemDto
{
    public int MedicationId { get; set; }
    public string Dose { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public string Duration { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public string? Instructions { get; set; }
}

public class CreatePrescriptionDto
{
    public int ConsultationId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePrescriptionItemDto> Items { get; set; } = new();
}
