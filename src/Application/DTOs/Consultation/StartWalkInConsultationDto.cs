using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Consultation;

public class StartWalkInConsultationDto
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }
}

public class StartWalkInConsultationResponseDto
{
    public int AppointmentId { get; set; }
    public int ConsultationId { get; set; }
}
