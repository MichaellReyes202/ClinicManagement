using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.ExamType;
public class ExamTypeCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DeliveryTime { get; set; }
    public decimal PricePaid { get; set; }
    public int SpecialtyId { get; set; }

}

public class ExamTypeUpdateDto : ExamTypeCreateDto
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}