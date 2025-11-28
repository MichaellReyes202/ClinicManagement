using System.Collections.Generic;

namespace Application.DTOs.Laboratory;

public class ExamOrderDto
{
    public int AppointmentId { get; set; }
    public int? ConsultationId { get; set; }
    public List<int> ExamTypeIds { get; set; } = new List<int>();
}
