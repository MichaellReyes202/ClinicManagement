using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.ExamType;
public class ExamTypeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DeliveryTime { get; set; }
    public decimal PricePaid { get; set; }
    public int SpecialtyId { get; set; }
    public bool IsActive { get; set; }
}
