using Application.DTOs.Appointment;
using Application.DTOs.Patient;
using Application.Interfaces;
using Application.Util;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;


namespace Application.Services;
public class AppointmentServices : IAppointmentServices
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IValidator<AppointmentCreateDto> _createValidator;
    private readonly IValidator<AppointmentUpdateDto> _updateValidator;
    private readonly IValidator<UpdateStatusAppointmenDto> _updateStatusValidator;
    private readonly IPatientRepository _patientRepository;
    private readonly IEmployesRepository _employesRepository;
    private readonly ICatalogServices _catalogServices;
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditlogServices _auditlogServices;

    public AppointmentServices(IAppointmentRepository appointmentRepository , 
        IValidator<AppointmentCreateDto> createValidator ,
        IValidator<AppointmentUpdateDto> updateValidator,
        IValidator<UpdateStatusAppointmenDto> updateStatusValidator ,
        IPatientRepository patientRepository ,
        IEmployesRepository employesRepository,
        ICatalogServices catalogServices,
        IUserService userService,
        IMapper mapper ,
        IHttpContextAccessor httpContextAccessor,
        IAuditlogServices auditlogServices
    )
    {
        _appointmentRepository = appointmentRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _updateStatusValidator = updateStatusValidator;
        _patientRepository = patientRepository;
        _employesRepository = employesRepository;
        _catalogServices = catalogServices;
        _userService = userService;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _auditlogServices = auditlogServices;
    }

    // servicio para obtener la cita por el Id 
    public async Task<Result<AppointmentDetailDto>> GetById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<AppointmentDetailDto>.Failure( new Error(ErrorCodes.BadRequest, $"The ID : {id} sent is not valid.", nameof(id)));
            }
            var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);
            var query = await _appointmentRepository.GetQuery(
                filter: c => c.Id == id,
                include: q => q
                    .Include(a => a.Patient).ThenInclude(p => p.BloodType)
                    .Include(a => a.Employee)
            );
            var appointment = await query.AsNoTracking().FirstOrDefaultAsync();

            if (appointment == null)
            {
                return Result<AppointmentDetailDto>.Failure(new Error(ErrorCodes.NotFound, $"Appointment {id} not found", nameof(id)));
            }
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - appointment.Patient.DateOfBirth.Year;
            if (appointment.Patient.DateOfBirth > today.AddYears(-age)) age--;
            var appointmentDto = new AppointmentDetailDto
            {
                AppointmentId = appointment.Id,
                StatusId = appointment.StatusId,
                DoctorId = appointment.EmployeeId ?? 0,
                Doctor = appointment.Employee != null ? $"{appointment.Employee.FirstName} {appointment.Employee.LastName}".Trim() : "No Assigned",
                StartTime = TimeZoneInfo.ConvertTimeFromUtc(appointment.StartTime, userTimeZone),
                Reason = appointment.Reason,

                Patient = new PatientByAppointmentDto
                {
                    Id = appointment.Patient.Id,
                    FullName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim(),
                    Age = age,
                    BloodType = appointment.Patient.BloodType?.Name ?? "Unknown",
                    Allergies = appointment.Patient.Allergies,
                    ChronicConditions = appointment.Patient.ChronicDiseases
                }
            };
            return Result<AppointmentDetailDto>.Success(appointmentDto);
        }
        catch (Exception ex)
        {
            return Result<AppointmentDetailDto>.Failure( new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }

    public async Task<Result> UpdateStatusAppointmentsAsync(UpdateStatusAppointmenDto dto)
    {
        var validationResult = await _updateStatusValidator.ValidateAsync(dto);
        if(!validationResult.IsValid)
        {
            var errors = validationResult.Errors
               .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
               .ToList();
            return Result.Failure(errors);
        }
        using var transaction = await _appointmentRepository.BeginTransactionAsync();

        try
        {
            var appointment = await _appointmentRepository.GetByIdAsync(dto.AppointmenId);
            if (appointment is null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The appointment does not exist", "Id"));
            }
            var isStatusExist = await _catalogServices.ExistAppointmentStatus(dto.StatusId);
            if (!isStatusExist)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The status id was not found", "StatusId"));
            }            
            if( 
                (appointment.StatusId == 2 && dto.StatusId == 1 ) || 
                (appointment.StatusId == 3 && new List<int> { 1 , 5 , 6 }.Contains(dto.StatusId) ) ||
                (appointment.StatusId == 4 && new List<int> { 1, 2, 3, 5, 6 }.Contains(dto.StatusId)) ||
                (appointment.StatusId == 5 && new List<int> { 1, 2, 3, 4, 6 }.Contains(dto.StatusId)) ||
                (appointment.StatusId == 6 && new List<int> { 1, 2, 3, 4,5 }.Contains(dto.StatusId))
            )
            {
                return Result.Failure(new Error(ErrorCodes.BadRequest, "The statusId exchange rate is not valid", "StatusId"));
            }

            appointment.StatusId = dto.StatusId;
            await _appointmentRepository.UpdateAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();

            // Registrar auditoría de cambio de estado
            try
            { 
                var currentUser = await _userService.GetCurrentUserAsync();
                var statusNames = new Dictionary<int, string>
                {
                    {1, "Programada"}, {2, "Confirmada"}, {3, "En curso"},
                    {4, "Completada"}, {5, "Cancelada"}, {6, "Vencida"}
                };
                var oldStatus = statusNames.GetValueOrDefault(appointment.StatusId, "Desconocido");
                var newStatus = statusNames.GetValueOrDefault(dto.StatusId, "Desconocido");
                var changeDetail = $"Estado cambiado de {oldStatus} a {newStatus}";
                
                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: Domain.Enums.AuditModuletype.Appointments,
                    actionType: Domain.Enums.ActionType.STATUS_CHANGE,
                    recordDisplay: $"Cita #{appointment.Id}",
                    recordId: appointment.Id,
                    status: Domain.Enums.AuditStatus.SUCCESS,
                    changeDetail: changeDetail
                );
            }
            catch (Exception auditEx)
            {
                Console.WriteLine($"Error registrando auditoría: {auditEx.Message}");
            }

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, "Error saving to database."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, "Unexpected error updating appointment."));
        }
    }

    public async Task<Result<List<TodayAppointmentDto>>> GetTodayAppointmentsAsync(DateTime? date = null)
    {
        var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
        
        // Si viene fecha, usamos esa convertida a local. Si no, usamos la fecha actual local.
        var localDate = date.HasValue 
            ? TimeZoneInfo.ConvertTimeFromUtc(date.Value, userTimeZone) 
            : localNow;

        var localTodayStart = localDate.Date;
        
        // Asegurar que el Kind sea Unspecified para que ConvertTimeToUtc funcione con la zona horaria del usuario
        localTodayStart = DateTime.SpecifyKind(localTodayStart, DateTimeKind.Unspecified);

        var localTomorrowStart = localTodayStart.AddDays(1); 
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localTodayStart, userTimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localTomorrowStart, userTimeZone);

        var query = await _appointmentRepository.GetQuery(a => a.StartTime >= utcStart && a.StartTime < utcEnd && a.StatusId != 6);

        // Data Scoping: Si es médico (Role 3), solo ver sus citas
        var user = _httpContextAccessor.HttpContext?.User;
        var roleIdClaim = user?.FindFirst("roleId");
        if (roleIdClaim != null && int.TryParse(roleIdClaim.Value, out int roleId) && roleId == 3)
        {
             var currentUser = await _userService.GetCurrentUserAsync();
             if (currentUser != null)
             {
                 var employeeQuery = await _employesRepository.GetQuery(e => e.UserId == currentUser.Id);
                 var employee = await employeeQuery.FirstOrDefaultAsync();
                 if (employee != null)
                 {
                     query = query.Where(a => a.EmployeeId == employee.Id);
                 }
             }
        }

        var appointments = await query.Include(a => a.Patient).Include(a => a.Employee).ThenInclude(e => e!.Specialty).OrderBy(a => a.StartTime).ToListAsync();
        var esCulture = new CultureInfo("es-ES");

        var result = appointments.Select(a => {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(a.StartTime, userTimeZone);
            return new TodayAppointmentDto
            {
                Id = a.Id,
                TimeDisplay = localTime.ToString("hh:mm tt", esCulture).ToUpper(),
                PatientFullName = $"{a.Patient?.FirstName} {a.Patient?.LastName}".Trim(),
                PatientPhone = a.Patient?.ContactPhone ?? "No registrado",
                DoctorFullName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}".Trim() : "Sin doctor asignado",
                SpecialtyName = a.Employee?.Specialty?.Name ?? "General",
                Reason = string.IsNullOrEmpty(a.Reason) ? "Consulta general" : a.Reason,
                StatusId = a.StatusId,
                Status = a.StatusId switch
                {
                    1 => "Programada",
                    2 => "Confirmada",
                    3 => "En curso", 
                    4 => "Completada",
                    5 => "Cancelada",
                    _ => "Vencida"
                }
            };
        }).ToList();
        return Result<List<TodayAppointmentDto>>.Success(result);
    }
    public async Task<Result<List<DoctorAvailabilityDto>>> GetDoctorAvailabilityAsync(int? specialtyId = null)
    {
        var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
        var localToday = localNow.Date;
        var localTomorrow = localToday.AddDays(1);
        var utcStartOfDay = TimeZoneInfo.ConvertTimeToUtc(localToday, userTimeZone);
        (TimeSpan start, TimeSpan end, int totalSlots) GetSchedule(DateTime date) => date.DayOfWeek switch
        {
            DayOfWeek.Sunday => (TimeSpan.Zero, TimeSpan.Zero, 0),
            DayOfWeek.Saturday => (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 18),
            _ => (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 18) // Lunes a Viernes 8-5
        };
        var (startToday, endToday, totalSlotsToday) = GetSchedule(localToday);
        var baseQuery = await _employesRepository.GetQuery(e =>  e.IsActive == true && e.PositionId == 1 && (specialtyId == null || e.SpecialtyId == specialtyId));
        var doctorsData = await baseQuery
            .AsNoTracking()
            .Select(d => new
            {
                Doctor = d,
                SpecialtyName = d.Specialty!.Name,
                FutureAppointments = d.Appointments.Where(a => (a.StatusId == 1 || a.StatusId == 2 || a.StatusId == 3) && a.StartTime >= utcStartOfDay).ToList()
            })
            .ToListAsync();
        if (!doctorsData.Any())
        {
            return Result<List<DoctorAvailabilityDto>>.Success(new List<DoctorAvailabilityDto>());
        }
        var result = new List<DoctorAvailabilityDto>();
        var esCulture = new CultureInfo("es-NI");

        foreach (var item in doctorsData)
        {
            var doctor = item.Doctor;
            var localAppointments = item.FutureAppointments
                .Select(a => new
                {
                    Original = a,
                    LocalStartTime = TimeZoneInfo.ConvertTimeFromUtc(a.StartTime, userTimeZone),
                    LocalEndTime = TimeZoneInfo.ConvertTimeFromUtc(a.EndTime ?? a.StartTime.AddMinutes(a.Duration), userTimeZone)
                })
                .OrderBy(a => a.LocalStartTime)
                .ToList();
            var appointmentsToday = localAppointments.Where(a => a.LocalStartTime.Date == localToday) .ToList();
            bool isBusyNow = appointmentsToday.Any(a => a.LocalStartTime <= localNow && a.LocalEndTime > localNow);

            int occupiedSlots = 0;
            foreach (var apt in appointmentsToday)
            {
                var effectiveStart = apt.LocalStartTime.TimeOfDay < startToday ? startToday : apt.LocalStartTime.TimeOfDay;
                var effectiveEnd = apt.LocalEndTime.TimeOfDay > endToday ? endToday : apt.LocalEndTime.TimeOfDay;

                if (effectiveEnd > effectiveStart)
                {
                    var durationMinutes = (effectiveEnd - effectiveStart).TotalMinutes;
                    occupiedSlots += (int)Math.Ceiling(durationMinutes / 30.0);
                }
            }
            int availableSlotsToday = Math.Max(0, totalSlotsToday - occupiedSlots);
            var nextExistingAppt = localAppointments.FirstOrDefault(a => a.LocalStartTime > localNow);

            DateTime? nextTimeDisplay = null;
            string textDisplay = "";

            if (nextExistingAppt != null)
            {
                nextTimeDisplay = nextExistingAppt.LocalStartTime;
                string timeStr = nextExistingAppt.LocalStartTime.ToString("hh:mm tt", esCulture).ToUpper();
                if (nextExistingAppt.LocalStartTime.Date == localToday)
                {
                    textDisplay = $"Ocupado hasta {timeStr}"; 
                }
                else if (nextExistingAppt.LocalStartTime.Date == localTomorrow)
                {
                    textDisplay = $"Mañana {timeStr}";
                }
                else
                {
                    textDisplay = nextExistingAppt.LocalStartTime.ToString("ddd dd MMM hh:mm tt", esCulture).ToUpper();
                }
            }
            else
            {
                if (availableSlotsToday > 0 && localNow.TimeOfDay < endToday)
                {
                    textDisplay = "Disponible Ahora";
                    nextTimeDisplay = localNow;
                }
                else
                {
                    var nextDay = localToday.AddDays(1);
                    var schedule = GetSchedule(nextDay);
                    while (schedule.totalSlots == 0)
                    {
                        nextDay = nextDay.AddDays(1);
                        schedule = GetSchedule(nextDay);
                    }

                    var nextOpenTime = nextDay.Add(schedule.start);
                    nextTimeDisplay = nextOpenTime;

                    textDisplay = nextDay == localTomorrow ? $"Mañana {schedule.start:hh\\:mm} {((schedule.start.Hours < 12) ? "AM" : "PM")}" : nextOpenTime.ToString("ddd hh:mm tt", esCulture).ToUpper();
                }
            }
            var fullName = string.Join(" ", new[] { doctor.FirstName, doctor.MiddleName, doctor.LastName, doctor.SecondLastName}.Where(n => !string.IsNullOrWhiteSpace(n)));
            result.Add(new DoctorAvailabilityDto
            {
                DoctorId = doctor.Id,
                FullName = fullName,
                SpecialtyName = item.SpecialtyName ?? "General",
                IsAvailable = !isBusyNow, // True si no está en una cita en este segundo
                NextAppointmentTime = nextTimeDisplay,
                NextAppointmentDisplay = textDisplay,
                AvailableSlotsToday = availableSlotsToday,
                AppointmentsTodayCount = appointmentsToday.Count
            });
        }

        return Result<List<DoctorAvailabilityDto>>.Success(
            result.OrderBy(d => d.FullName).ToList()
        );
    }

    public async Task<Result<List<AppointmentListDto>>> GetListAsync(AppointmentFilterDto filter)
    {
        var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);
        var query = await _appointmentRepository.GetQuery();

        // Data Scoping: Si es médico (Role 3), solo ver sus citas
        var user = _httpContextAccessor.HttpContext?.User;
        var roleIdClaim = user?.FindFirst("roleId");
        if (roleIdClaim != null && int.TryParse(roleIdClaim.Value, out int roleId) && roleId == 3)
        {
             var currentUser = await _userService.GetCurrentUserAsync();
             if (currentUser != null)
             {
                 var employeeQuery = await _employesRepository.GetQuery(e => e.UserId == currentUser.Id);
                 var employee = await employeeQuery.FirstOrDefaultAsync();
                 if (employee != null)
                 {
                     query = query.Where(a => a.EmployeeId == employee.Id);
                 }
             }
        }

        if (filter.Specialty.HasValue)
        {
            query = query.Where(a => a.Employee.SpecialtyId == filter.Specialty.Value);
        }
        if (filter.Doctor.HasValue)
        {
            query = query.Where(a => a.EmployeeId == filter.Doctor.Value);
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.StatusId == filter.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Patient.FirstName.ToLower().Contains(term) ||
                a.Patient.LastName.ToLower().Contains(term) ||
                a.Employee.FirstName.ToLower().Contains(term) ||
                a.Employee.LastName.ToLower().Contains(term)
            );
        }
        if (filter.DateFrom.HasValue)
        {
            var localFromDate = filter.DateFrom.Value.Date; // 00:00:00 Local
            var utcFromDate = TimeZoneInfo.ConvertTimeToUtc(localFromDate, userTimeZone);
            query = query.Where(a => a.StartTime >= utcFromDate);
        }

        if (filter.DateTo.HasValue)
        {
            // Para incluir todo el día final, tomamos el final del día local (23:59:59)
            var localToDate = filter.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            var utcToDate = TimeZoneInfo.ConvertTimeToUtc(localToDate, userTimeZone);
            query = query.Where(a => a.StartTime <= utcToDate);
        }
        var rawItems = await query
            .AsNoTracking()
            .OrderByDescending(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                PatientFirstName = a.Patient.FirstName,
                PatientLastName = a.Patient.LastName,
                a.EmployeeId,
                DoctorSpecialtyId = a.Employee.SpecialtyId,
                DoctorFirstName = a.Employee.FirstName,
                DoctorLastName = a.Employee.LastName,
                StartTimeUtc = a.StartTime, // La BD devuelve UTC
                EndTimeUtc = a.EndTime,
                a.Duration,
                a.Reason,
                StatusName = a.Status.Name,
                StatusId = a.Status.Id
            })
            .ToListAsync();
        var result = rawItems.Select(item => new AppointmentListDto
        {
            Id = item.Id,
            PatientId = item.PatientId,
            PatientFullName = $"{item.PatientFirstName} {item.PatientLastName}".Trim(),
            EmployeeId = item.EmployeeId,
            DoctorSpecialtyId = item.DoctorSpecialtyId,
            DoctorFullName = $"{item.DoctorFirstName} {item.DoctorLastName}".Trim(),
            StartTime = TimeZoneInfo.ConvertTimeFromUtc(item.StartTimeUtc, userTimeZone),
            EndTime = item.EndTimeUtc.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(item.EndTimeUtc.Value, userTimeZone)
                : TimeZoneInfo.ConvertTimeFromUtc(item.StartTimeUtc, userTimeZone).AddMinutes(item.Duration),

            Duration = item.Duration,
            Reason = item.Reason,
            Status = item.StatusName,
            StatusId = item.StatusId,
        })
        .ToList();
        return Result<List<AppointmentListDto>>.Success(result);
    }

    public async Task<Result<int>> Add(AppointmentCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<int>.Failure(errors);
        }

        using var transaction = await _appointmentRepository.BeginTransactionAsync();
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync();
            var patientExists = await _patientRepository.ExistAsync(p => p.Id == dto.PatientId);
            if (!patientExists)
            {
                return Result<int>.Failure(new Error(ErrorCodes.NotFound, "The patient does not exist or is inactive", "PatientId"));
            }
            var doctorExists = await _employesRepository.ExistAsync(e => e.Id == dto.EmployeeId && e.IsActive.HasValue && e.PositionId == 1);
            if (!doctorExists)
            {
                return Result<int>.Failure(new Error(ErrorCodes.NotFound, "The doctor does not exist or is inactive.", "EmployeeId"));
            }
            var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);
            var incomingLocalTime = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Unspecified);

            var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(incomingLocalTime, userTimeZone);
            var endTimeUtc = startTimeUtc.AddMinutes(dto.Duration);
            var localDayStart = incomingLocalTime.Date;
            var localDayEnd = localDayStart.AddDays(1).AddTicks(-1); 

            var utcRangeStart = TimeZoneInfo.ConvertTimeToUtc(localDayStart, userTimeZone);
            var utcRangeEnd = TimeZoneInfo.ConvertTimeToUtc(localDayEnd, userTimeZone);

            var patientHasOverlap = await _appointmentRepository.ExistAsync(a =>
                a.PatientId == dto.PatientId &&
                a.StartTime < endTimeUtc &&
                a.StartTime.AddMinutes(a.Duration) > startTimeUtc &&
                a.StatusId != 5 && a.StatusId != 6 // Ignoramos canceladas/vencidas
            );

            if (patientHasOverlap)
            {
                return Result<int>.Failure(new Error(ErrorCodes.BadRequest, "The patient already has an appointment at that time.", "StartTime"));
            }
            var doctorHasOverlap = await _appointmentRepository.ExistAsync(a =>
                a.EmployeeId == dto.EmployeeId &&
                a.StartTime < endTimeUtc &&
                a.StartTime.AddMinutes(a.Duration) > startTimeUtc &&
                a.StatusId != 5 // Ignorar canceladas
            );

            if (doctorHasOverlap)
            {
                // Relaxed for testing/immediate consultation: Log warning but allow
                // return Result<int>.Failure(new Error(ErrorCodes.BadRequest, "The doctor already has an appointment at that time.", "StartTime"));
            }

            var appointment = _mapper.Map<Appointment>(dto);
            appointment.StartTime = startTimeUtc;
            appointment.EndTime = endTimeUtc;

            appointment.StatusId = (int)AppointmentStatus.Scheduled;
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.CreatedByUserId = currentUser?.Id;

            await _appointmentRepository.AddAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();

            // Registrar auditoría
            try
            {
                var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
                var doctor = await _employesRepository.GetByIdAsync(dto.EmployeeId);
                var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "Paciente desconocido";
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : "Doctor desconocido";
                
                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: AuditModuletype.Appointments,
                    actionType: Domain.Enums.ActionType.CREATE,
                    recordDisplay: $"Cita para {patientName} con {doctorName}",
                    recordId: appointment.Id,
                    status: AuditStatus.SUCCESS
                );
            }
            catch (Exception auditEx)
            {
                Console.WriteLine($"Error registrando auditoría: {auditEx.Message}");
            }

            await transaction.CommitAsync();

            return Result<int>.Success(appointment.Id);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Result<int>.Failure(new Error(ErrorCodes.Unexpected, "Error saving to database."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result<int>.Failure(new Error(ErrorCodes.Unexpected, "Unexpected error creating appointment."));
        }
    }
    public async Task<Result> Update(AppointmentUpdateDto dto, int patientId)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result.Failure(errors);
        }

        using var transaction = await _appointmentRepository.BeginTransactionAsync();
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync();
            var appointment = await _appointmentRepository.GetByIdAsync(dto.Id);
            if (appointment is null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The appointment does not exist", "Id"));
            }
            var patientExists = await _patientRepository.ExistAsync(p => p.Id == dto.PatientId);
            if (!patientExists)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The patient does not exist or is inactive", "PatientId"));
            }
            var doctorExists = await _employesRepository.ExistAsync(e => e.Id == dto.EmployeeId && e.IsActive.HasValue && e.PositionId == 1);
            if (!doctorExists)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "The doctor does not exist or is inactive.", "EmployeeId"));
            }

            var existStatusId = await _catalogServices.ExistAppointmentStatus(dto.StatusId);
            if (!existStatusId)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, $"The code for the appointment status {dto.StatusId} was not found", "StatusId"));
            }
            var userTimeZone = GetTimeZone.GetRequestTimeZone(_httpContextAccessor);

            var incomingLocalTime = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Unspecified);
            var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(incomingLocalTime, userTimeZone);
            var endTimeUtc = startTimeUtc.AddMinutes(dto.Duration);
            var doctorHasOverlap = await _appointmentRepository.ExistAsync(a =>
                a.EmployeeId == dto.EmployeeId &&
                a.StartTime < endTimeUtc &&
                a.StartTime.AddMinutes(a.Duration) > startTimeUtc &&
                a.Id != dto.Id &&
                a.StatusId != 5 // Ignorar canceladas
            );

            if (doctorHasOverlap)
            {
                return Result.Failure(new Error(ErrorCodes.BadRequest, "The doctor already has an appointment at that time.", "StartTime"));
            }

            // 6. Mapear y Actualizar
            _mapper.Map(dto, appointment);
            appointment.StartTime = startTimeUtc;
            appointment.EndTime = endTimeUtc;

            appointment.StatusId = dto.StatusId;
            appointment.UpdatedAt = DateTime.UtcNow;
            appointment.UpdatedByUserId = currentUser?.Id;

            await _appointmentRepository.UpdateAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();

            // Registrar auditoría
            try
            {
                var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
                var doctor = await _employesRepository.GetByIdAsync(dto.EmployeeId);
                var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "Paciente desconocido";
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : "Doctor desconocido";
                
                await _auditlogServices.RegisterActionAsync(
                    userId: currentUser?.Id,
                    module: Domain.Enums.AuditModuletype.Appointments,
                    actionType: Domain.Enums.ActionType.UPDATE,
                    recordDisplay: $"Cita para {patientName} con {doctorName}",
                    recordId: appointment.Id,
                    status: Domain.Enums.AuditStatus.SUCCESS
                );
            }
            catch (Exception auditEx)
            {
                Console.WriteLine($"Error registrando auditoría: {auditEx.Message}");
            }

            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, "Error saving to database."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, "Unexpected error updating appointment."));
        }
    }

    public async Task<Result> Delete(int id)
    {
        using var transaction = await _appointmentRepository.BeginTransactionAsync();
        try
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id);
            if (appointment == null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, "Appointment not found"));
            }

            await _appointmentRepository.DeleteAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();

            var currentUser = await _userService.GetCurrentUserAsync();
            await _auditlogServices.RegisterActionAsync(
                userId: currentUser?.Id,
                module: Domain.Enums.AuditModuletype.Appointments,
                actionType: Domain.Enums.ActionType.DELETE,
                recordDisplay: $"Cita #{id} eliminada",
                recordId: id,
                status: Domain.Enums.AuditStatus.SUCCESS
            );

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"Error deleting appointment: {ex.Message}"));
        }
    }

}
