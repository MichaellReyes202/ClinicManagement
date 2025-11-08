using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.ExamType;
public class ExamTypeListDto : ExamTypeResponseDto
{
    public required string SpecialtyName { get; set; }
}
