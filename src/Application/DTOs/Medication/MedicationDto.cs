namespace Application.DTOs.Medication;

public class MedicationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? Presentation { get; set; }
    public string? Concentration { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool? IsActive { get; set; }
}
