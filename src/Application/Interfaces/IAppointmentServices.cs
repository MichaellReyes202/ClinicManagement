

using Application.DTOs.Appointment;
using Domain.Errors;

namespace Application.Interfaces;

public interface IAppointmentServices
{
    Task<Result<AppointmentResponseDto>> Add(AppointmentCreateDto dto);
    Task<Result<List<DoctorAvailabilityDto>>> GetDoctorAvailabilityAsync();
    Task<Result<List<AppointmentListDto>>> GetListAsync();
    Task<Result> Update(AppointmentUpdateDto dto, int patientId);
}
