using Application.DTOs.Schedule;
using Application.Interfaces;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ScheduleService : IScheduleService
    {
        private static readonly string[] DayNames = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

        private readonly IGenericRepository<ClinicSchedule> _clinicScheduleRepo;
        private readonly IGenericRepository<EmployeeSchedule> _employeeScheduleRepo;
        private readonly IGenericRepository<Employee> _employeeRepo;

        public ScheduleService(
            IGenericRepository<ClinicSchedule> clinicScheduleRepo,
            IGenericRepository<EmployeeSchedule> employeeScheduleRepo,
            IGenericRepository<Employee> employeeRepo)
        {
            _clinicScheduleRepo = clinicScheduleRepo;
            _employeeScheduleRepo = employeeScheduleRepo;
            _employeeRepo = employeeRepo;
        }

        // ── Horario general de la clínica ──────────────────────────────────

        public async Task<Result<List<ClinicScheduleDto>>> GetClinicSchedulesAsync()
        {
            var query = await _clinicScheduleRepo.GetQuery(orderBy: q => q.OrderBy(s => s.DayOfWeek));
            var list = await query.ToListAsync();
            return Result<List<ClinicScheduleDto>>.Success(list.Select(MapClinic).ToList());
        }

        public async Task<Result<ClinicScheduleDto>> UpdateClinicScheduleAsync(int id, UpdateClinicScheduleDto dto)
        {
            var schedule = await _clinicScheduleRepo.GetByIdAsync(id);
            if (schedule == null)
                return Result<ClinicScheduleDto>.Failure(new Error(ErrorCodes.NotFound, "Horario no encontrado."));

            if (!TimeOnly.TryParse(dto.OpenTime, out var open) || !TimeOnly.TryParse(dto.CloseTime, out var close))
                return Result<ClinicScheduleDto>.Failure(new Error(ErrorCodes.BadRequest, "Formato de hora inválido. Use HH:mm."));

            if (close <= open)
                return Result<ClinicScheduleDto>.Failure(new Error(ErrorCodes.BadRequest, "La hora de cierre debe ser posterior a la hora de apertura."));

            schedule.IsOpen = dto.IsOpen;
            schedule.OpenTime = open;
            schedule.CloseTime = close;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _clinicScheduleRepo.UpdateAsync(schedule);
            await _clinicScheduleRepo.SaveChangesAsync();

            return Result<ClinicScheduleDto>.Success(MapClinic(schedule));
        }

        // ── Horario por empleado/doctor ────────────────────────────────────

        public async Task<Result<List<EmployeeScheduleDto>>> GetEmployeeSchedulesAsync(int employeeId)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            if (employee == null)
                return Result<List<EmployeeScheduleDto>>.Failure(new Error(ErrorCodes.NotFound, "Empleado no encontrado."));

            var query = await _employeeScheduleRepo.GetQuery(
                filter: s => s.EmployeeId == employeeId,
                orderBy: q => q.OrderBy(s => s.DayOfWeek));

            var list = await query.ToListAsync();
            return Result<List<EmployeeScheduleDto>>.Success(list.Select(MapEmployee).ToList());
        }

        public async Task<Result<EmployeeScheduleDto>> UpsertEmployeeScheduleAsync(int employeeId, UpsertEmployeeScheduleDto dto)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            if (employee == null)
                return Result<EmployeeScheduleDto>.Failure(new Error(ErrorCodes.NotFound, "Empleado no encontrado."));

            if (!TimeOnly.TryParse(dto.StartTime, out var start) || !TimeOnly.TryParse(dto.EndTime, out var end))
                return Result<EmployeeScheduleDto>.Failure(new Error(ErrorCodes.BadRequest, "Formato de hora inválido. Use HH:mm."));

            if (end <= start)
                return Result<EmployeeScheduleDto>.Failure(new Error(ErrorCodes.BadRequest, "La hora de fin debe ser posterior a la hora de inicio."));

            // Buscar si ya existe un registro para ese día
            var query = await _employeeScheduleRepo.GetQuery(
                filter: s => s.EmployeeId == employeeId && s.DayOfWeek == dto.DayOfWeek);
            var existing = await query.FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.IsAvailable = dto.IsAvailable;
                existing.StartTime = start;
                existing.EndTime = end;
                existing.UpdatedAt = DateTime.UtcNow;
                await _employeeScheduleRepo.UpdateAsync(existing);
                await _employeeScheduleRepo.SaveChangesAsync();
                return Result<EmployeeScheduleDto>.Success(MapEmployee(existing));
            }
            else
            {
                var newSchedule = new EmployeeSchedule
                {
                    EmployeeId = employeeId,
                    DayOfWeek = dto.DayOfWeek,
                    IsAvailable = dto.IsAvailable,
                    StartTime = start,
                    EndTime = end,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                await _employeeScheduleRepo.AddAsync(newSchedule);
                await _employeeScheduleRepo.SaveChangesAsync();
                return Result<EmployeeScheduleDto>.Success(MapEmployee(newSchedule));
            }
        }

        public async Task<Result<bool>> UpdateEmployeeAppointmentDurationAsync(int employeeId, UpdateEmployeeAppointmentDurationDto dto)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            if (employee == null)
                return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "Empleado no encontrado."));

            if (dto.AppointmentDurationMinutes < 5 || dto.AppointmentDurationMinutes > 120)
                return Result<bool>.Failure(new Error(ErrorCodes.BadRequest, "La duración debe estar entre 5 y 120 minutos."));

            employee.AppointmentDurationMinutes = dto.AppointmentDurationMinutes;
            await _employeeRepo.UpdateAsync(employee);
            await _employeeRepo.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        // ── Mappers ────────────────────────────────────────────────────────

        private static ClinicScheduleDto MapClinic(ClinicSchedule s) => new()
        {
            Id = s.Id,
            DayOfWeek = s.DayOfWeek,
            DayName = s.DayName,
            IsOpen = s.IsOpen,
            OpenTime = s.OpenTime.ToString("HH:mm"),
            CloseTime = s.CloseTime.ToString("HH:mm"),
        };

        private static EmployeeScheduleDto MapEmployee(EmployeeSchedule s) => new()
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            DayOfWeek = s.DayOfWeek,
            DayName = DayNames[s.DayOfWeek],
            IsAvailable = s.IsAvailable,
            StartTime = s.StartTime.ToString("HH:mm"),
            EndTime = s.EndTime.ToString("HH:mm"),
        };
    }
}
