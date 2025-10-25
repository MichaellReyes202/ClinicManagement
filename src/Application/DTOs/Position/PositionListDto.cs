
namespace Application.DTOs.Position
{
    public class PositionListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int Employees { get; set; }
    }
}
