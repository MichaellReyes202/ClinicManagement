using Application.DTOs.Schedule;
using Domain.Errors;

namespace Application.Interfaces
{
    public interface IScheduleService
    {
        // --- Horario general de la clínica ---
        Task<Result<List<ClinicScheduleDto>>> GetClinicSchedulesAsync();
        Task<Result<ClinicScheduleDto>> UpdateClinicScheduleAsync(int id, UpdateClinicScheduleDto dto);

        // --- Horario por empleado/doctor ---
        Task<Result<List<EmployeeScheduleDto>>> GetEmployeeSchedulesAsync(int employeeId);
        Task<Result<EmployeeScheduleDto>> UpsertEmployeeScheduleAsync(int employeeId, UpsertEmployeeScheduleDto dto);
        Task<Result<bool>> UpdateEmployeeAppointmentDurationAsync(int employeeId, UpdateEmployeeAppointmentDurationDto dto);
    }
}
