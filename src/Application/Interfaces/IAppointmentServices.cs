

using Application.DTOs.Appointment;
using Domain.Errors;

namespace Application.Interfaces;

public interface IAppointmentServices
{
    Task<Result<AppointmentResponseDto>> Add(AppointmentCreateDto dto);
    Task<Result<List<AppointmentListDto>>> GetListAsync(AppointmentFilterDto filter);
    Task<Result> Update(AppointmentUpdateDto dto, int patientId);

    Task<Result<List<TodayAppointmentDto>>> GetTodayAppointmentsAsync();
    Task<Result<List<DoctorAvailabilityDto>>> GetDoctorAvailabilityAsync(int? specialtyId = null);
    Task<Result> UpdateStatusAppointmentsAsync(UpdateStatusAppointmenDto dto);
    Task<Result<AppointmentDetailDto>> GetById(int id);
}
