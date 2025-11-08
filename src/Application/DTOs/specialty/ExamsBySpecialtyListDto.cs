using Application.DTOs.ExamType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Specialty;
public class ExamsBySpecialtyListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual ICollection<ExamTypeListDto> ExamTypes { get; set; } = new List<ExamTypeListDto>();
}
