namespace Domain.Entities;

public class AppointmentReminder
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public virtual Appointment Appointment { get; set; } = null!;
}
