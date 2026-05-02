namespace Application.DTOs.Schedule
{
    // DTO de respuesta para el horario de la clínica
    public class ClinicScheduleDto
    {
        public int Id { get; set; }
        public short DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public string OpenTime { get; set; } = string.Empty;   // "HH:mm"
        public string CloseTime { get; set; } = string.Empty;  // "HH:mm"
    }

    // DTO para actualizar un día del horario de la clínica
    public class UpdateClinicScheduleDto
    {
        public bool IsOpen { get; set; }
        public string OpenTime { get; set; } = string.Empty;   // "HH:mm"
        public string CloseTime { get; set; } = string.Empty;  // "HH:mm"
    }

    // DTO de respuesta para el horario de un empleado/doctor
    public class EmployeeScheduleDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public short DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string StartTime { get; set; } = string.Empty;  // "HH:mm"
        public string EndTime { get; set; } = string.Empty;    // "HH:mm"
    }

    // DTO para crear o actualizar el horario de un doctor para un día
    public class UpsertEmployeeScheduleDto
    {
        public short DayOfWeek { get; set; }
        public bool IsAvailable { get; set; }
        public string StartTime { get; set; } = string.Empty;  // "HH:mm"
        public string EndTime { get; set; } = string.Empty;    // "HH:mm"
    }

    // DTO para actualizar la duración estándar de cita de un empleado
    public class UpdateEmployeeAppointmentDurationDto
    {
        public int AppointmentDurationMinutes { get; set; }
    }
}
