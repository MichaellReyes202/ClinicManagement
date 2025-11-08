

namespace Application.DTOs.ExamType;
public class DoctorBySpecialtyDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<OptionDto> Doctors { get; set; } = new List<OptionDto>();
}
