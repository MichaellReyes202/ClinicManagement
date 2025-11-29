using System;

namespace Application.DTOs.Laboratory;

public class ExamPendingDto
{
    public int Id { get; set; }
    public int ExamTypeId { get; set; }
    public string ExamTypeName { get; set; } = null!;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = null!;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = null!;
    public string? Results { get; set; }
    public DateTime CreatedAt { get; set; }
    public int SpecialtyId { get; set; }
    public string SpecialtyName { get; set; } = null!;
}
