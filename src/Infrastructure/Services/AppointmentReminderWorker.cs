using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;

namespace Infrastructure.Services;

public sealed class AppointmentReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderWorker> _logger;

    public AppointmentReminderWorker(IServiceScopeFactory scopeFactory, ILogger<AppointmentReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try { await ProcessRemindersAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Error al procesar recordatorios de citas."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var now = DateTime.UtcNow;

        // Encola las citas activas de las próximas 24 horas; la restricción única evita duplicados
        // incluso cuando hay varias instancias del worker.
        var upcoming = await db.Appointments.AsNoTracking()
            .Where(a => (a.StatusId == 1 || a.StatusId == 2) && a.StartTime > now && a.StartTime <= now.AddHours(24))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        var existingIds = await db.AppointmentReminders.AsNoTracking()
            .Where(r => upcoming.Contains(r.AppointmentId))
            .Select(r => r.AppointmentId)
            .ToHashSetAsync(cancellationToken);

        foreach (var appointmentId in upcoming.Where(id => !existingIds.Contains(id)))
        {
            var appointment = await db.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
            if (appointment == null) continue;
            db.AppointmentReminders.Add(new AppointmentReminder
            {
                AppointmentId = appointmentId,
                ScheduledAtUtc = appointment.StartTime.AddHours(-24) < now ? now : appointment.StartTime.AddHours(-24),
                CreatedAtUtc = now
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        var due = await db.AppointmentReminders
            .Include(r => r.Appointment).ThenInclude(a => a.Patient)
            .Include(r => r.Appointment).ThenInclude(a => a.Employee)
            .Where(r => r.SentAtUtc == null && r.ScheduledAtUtc <= now && r.Attempts < 5 &&
                        (r.Appointment.StatusId == 1 || r.Appointment.StatusId == 2))
            .OrderBy(r => r.ScheduledAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);
        var mailer = scope.ServiceProvider.GetRequiredService<IEmailService>();

        foreach (var reminder in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reminder.Attempts++;
            try
            {
                var patient = reminder.Appointment.Patient;
                if (string.IsNullOrWhiteSpace(patient?.ContactEmail))
                    throw new InvalidOperationException("El paciente no tiene correo de contacto registrado.");

                var localTime = TimeZoneInfo.ConvertTimeFromUtc(reminder.Appointment.StartTime, TZConvert.GetTimeZoneInfo("America/Managua"));
                var doctorName = reminder.Appointment.Employee == null
                    ? "el equipo médico"
                    : $"Dr./Dra. {reminder.Appointment.Employee.FirstName} {reminder.Appointment.Employee.LastName}".Trim();
                var body = $"<p>Hola {System.Net.WebUtility.HtmlEncode(patient.FirstName)},</p>" +
                    $"<p>Te recordamos tu cita médica el <strong>{localTime:dd/MM/yyyy} a las {localTime:hh:mm tt}</strong> con {System.Net.WebUtility.HtmlEncode(doctorName)}.</p>" +
                    "<p>Si necesitas cambiar o cancelar tu cita, comunícate con la clínica.</p>";
                await mailer.SendEmailAsync(patient.ContactEmail, "Recordatorio de cita médica", body);
                reminder.SentAtUtc = DateTime.UtcNow;
                reminder.LastError = null;
            }
            catch (Exception ex)
            {
                reminder.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                _logger.LogWarning(ex, "No se pudo enviar el recordatorio de la cita {AppointmentId} (intento {Attempt}).", reminder.AppointmentId, reminder.Attempts);
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
