using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Appointment;
using Application.DTOs.Patient;
using Application.DTOs.Schedule;
using Application.Interfaces;
using Application.Util;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Infrastructure.Plugins;

public class ClinicPlugin
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClinicPlugin(IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor)
    {
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    private bool HasAnyView(params string[] viewNames)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true &&
               viewNames.Any(viewName => user.HasClaim("view", viewName));
    }

    [KernelFunction, Description("Obtiene la lista de citas médicas agendadas para el día de hoy o para una fecha específica. Permite al personal y doctores consultar la agenda.")]
    public async Task<string> ObtenerCitasDelDia(
        [Description("Fecha a consultar en formato YYYY-MM-DD (ejemplo: 2026-09-14). Opcional, si no se especifica se consulta la fecha actual.")] string? fecha = null)
    {
        if (!HasAnyView("Citas - Hoy", "Citas - Agendar", "Consultas - Activa", "Consultas - Historial"))
        {
            return "Acceso denegado: No tienes permisos para consultar citas.";
        }

        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            if (DateTime.TryParseExact(fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                parsedDate = dt;
            }
            else
            {
                return "Formato de fecha inválido. Se requiere el formato YYYY-MM-DD (ejemplo: 2026-09-14).";
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var appointmentServices = scope.ServiceProvider.GetRequiredService<IAppointmentServices>();

        var result = await appointmentServices.GetTodayAppointmentsAsync(parsedDate);
        if (result.IsFailure || result.Value == null)
        {
            return $"No se pudieron obtener las citas: {result.Error?.Description ?? "Error al consultar las citas."}";
        }

        var list = result.Value.Take(25).Select(a => new
        {
            a.Id,
            Hora = a.TimeDisplay,
            Paciente = a.PatientFullName,
            TelefonoPaciente = a.PatientPhone,
            Doctor = a.DoctorFullName,
            Especialidad = a.SpecialtyName,
            Motivo = a.Reason,
            Estado = a.Status
        }).ToList();

        if (!list.Any())
        {
            return $"No se encontraron citas agendadas para la fecha {(parsedDate.HasValue ? parsedDate.Value.ToString("yyyy-MM-dd") : "de hoy")}.";
        }

        return JsonSerializer.Serialize(list);
    }

    [KernelFunction, Description("Busca médicos activos por nombre, especialidad o DNI. Si recibe la solicitud completa, extrae el DNI asociado al doctor/médico, no el DNI del paciente. Devuelve la próxima fecha y hora futura disponible; no confundas una disponibilidad futura con si el médico está atendiendo en este instante. Devuelve la duración configurada.")]
    public async Task<string> BuscarMedicosParaCita(
        [Description("Nombre, especialidad o DNI completo del médico. Si se proporciona un DNI, envíalo aquí sin modificarlo.")] string? terminoBusqueda = null)
    {
        if (!HasAnyView("Citas - Agendar", "Consultas - Crear"))
        {
            return "Acceso denegado: No tienes permisos para consultar médicos para citas.";
        }

        using var scope = _scopeFactory.CreateScope();
        var appointmentServices = scope.ServiceProvider.GetRequiredService<IAppointmentServices>();
        var term = terminoBusqueda?.Trim();
        var dni = ExtractDniForRole(term, @"\b(?:doctor|m[eé]dico)\b") ?? ExtractDni(term);
        var isDniSearch = dni != null;
        var result = await appointmentServices.GetDoctorAvailabilityAsync(dni: dni);
        if (result.IsFailure || result.Value == null)
        {
            return $"Error al consultar médicos: {result.Error?.Description ?? "No se pudo obtener la lista de médicos."}";
        }

        var doctors = result.Value.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(term) && !isDniSearch)
        {
            doctors = doctors.Where(doctor =>
                doctor.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                doctor.SpecialtyName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var list = doctors.Take(25).Select(doctor => new
        {
            doctor.DoctorId,
            doctor.FullName,
            doctor.SpecialtyName,
            doctor.NextAppointmentDisplay,
            doctor.AppointmentDurationMinutes
        }).ToList();

        return list.Count == 0
            ? isDniSearch
                ? "No se encontró un médico activo con el DNI proporcionado."
                : $"No se encontraron médicos que coincidan con '{terminoBusqueda}'."
            : JsonSerializer.Serialize(list);
    }

    private static string? ExtractDni(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = Regex.Match(
            value,
            @"(?<![A-Z0-9])\d{3}[\s-]?\d{6}[\s-]?\d{4}[\s-]?[A-Z](?![A-Z0-9])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return null;
        }

        return new string(match.Value
            .Where(char.IsAsciiLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string? ExtractDniForRole(string? value, string rolePattern)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var roleMatch = Regex.Match(
            value,
            rolePattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!roleMatch.Success)
        {
            return null;
        }

        var roleContext = value[(roleMatch.Index + roleMatch.Length)..];
        var nextRole = Regex.Match(
            roleContext,
            @"\b(?:paciente|patient|doctor|m[eé]dico|medico)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (nextRole.Success)
        {
            roleContext = roleContext[..nextRole.Index];
        }

        return ExtractDni(roleContext);
    }

    [KernelFunction, Description("Crea y guarda una cita médica real en el sistema. Llama esta herramienta únicamente después de que el usuario haya confirmado explícitamente el resumen final y estén confirmados pacienteId, medicoId, fechaHora, duración y motivo. fechaHora debe ser la hora local del usuario en formato YYYY-MM-DDTHH:mm, sin zona horaria. No afirmes que la cita fue agregada a menos que esta herramienta devuelva éxito con un número de cita.")]
    public async Task<string> AgendarCita(
        [Description("ID exacto del paciente obtenido de BuscarPacientes.")] int pacienteId,
        [Description("ID exacto del médico obtenido de BuscarMedicosParaCita.")] int medicoId,
        [Description("Fecha y hora local en formato YYYY-MM-DDTHH:mm, por ejemplo 2026-11-05T09:30.")] string fechaHora,
        [Description("Duración en minutos, entre 5 y 150. Usa 30 si el usuario no indicó otra duración.")] int duracionMinutos = 30,
        [Description("Motivo de la cita indicado por el usuario.")] string? motivo = null)
    {
        if (!HasAnyView("Citas - Agendar", "Consultas - Crear"))
        {
            return "Acceso denegado: No tienes permisos para agendar citas.";
        }

        if (!TryParseLocalAppointmentDate(fechaHora, out var localStartTime))
        {
            return "Error: La fecha y hora deben usar el formato YYYY-MM-DDTHH:mm, por ejemplo 2026-11-05T09:30.";
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        if (!await HasExplicitAppointmentConfirmationAsync(dbContext))
        {
            return "No se creó la cita. Primero debo presentar el resumen y recibir tu confirmación explícita.";
        }

        var executionTracker = _httpContextAccessor.HttpContext?.RequestServices
            .GetService<IChatToolExecutionTracker>();
        var successfulSearches = executionTracker?.GetExecutions()
            .Where(execution => execution.IsSuccess)
            .Select(execution => execution.ToolName)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
        if (!successfulSearches.Contains("ClinicPlugin.BuscarPacientes") ||
            !successfulSearches.Contains("ClinicPlugin.BuscarMedicosParaCita"))
        {
            return "No se creó la cita porque aún no se validaron los registros del paciente y del médico en esta confirmación.";
        }

        var appointmentServices = scope.ServiceProvider.GetRequiredService<IAppointmentServices>();
        var result = await appointmentServices.Add(new AppointmentCreateDto
        {
            PatientId = pacienteId,
            EmployeeId = medicoId,
            StartTime = localStartTime,
            Duration = duracionMinutos,
            Reason = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim()
        });

        if (result.IsFailure || result.Value is not > 0)
        {
            var validationMessages = result.Error?.ValidationErrors?
                .Select(error => TranslateAppointmentValidation(error.ErrorMessage))
                .ToList();
            if (validationMessages?.Count > 0)
            {
                return $"Error de validación: {string.Join(" ", validationMessages)}";
            }

            return $"Error al agendar la cita: {TranslateAppointmentValidation(result.Error?.Description ?? "No se pudo guardar la cita.")}";
        }

        string? emailStatus = null;
        try
        {
            var patientRepository = scope.ServiceProvider.GetRequiredService<Domain.Interfaces.IPatientRepository>();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<Domain.Interfaces.IEmployesRepository>();
            var patient = await patientRepository.GetByIdAsync(pacienteId);
            var doctor = await employeeRepository.GetByIdAsync(medicoId);
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            if (!string.IsNullOrWhiteSpace(patient?.ContactEmail))
            {
                var emailBody = $"<p>Hola {System.Net.WebUtility.HtmlEncode(patient.FirstName)},</p>" +
                    $"<p>Tu cita ha sido registrada para el <strong>{localStartTime:dd/MM/yyyy} a las {localStartTime:hh:mm tt}</strong>.</p>" +
                    $"<p>Médico: {System.Net.WebUtility.HtmlEncode(doctor == null ? "Por asignar" : $"{doctor.FirstName} {doctor.LastName}")}.</p>" +
                    "<p>Si necesitas cambiar o cancelar tu cita, comunícate con la clínica.</p>";
                await emailService.SendEmailAsync(patient.ContactEmail, "Confirmación de cita médica", emailBody);
                emailStatus = "confirmacionCorreoEnviada";
            }
            else
            {
                emailStatus = "sinCorreoRegistrado";
            }
        }
        catch
        {
            emailStatus = "noSePudoEnviarCorreo";
        }

        return JsonSerializer.Serialize(new
        {
            exito = true,
            mensaje = "La cita fue guardada correctamente.",
            citaId = result.Value,
            pacienteId,
            medicoId,
            fechaHora = localStartTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            duracionMinutos,
            motivo,
            notificacion = emailStatus
        });
    }

    private async Task<bool> HasExplicitAppointmentConfirmationAsync(ClinicDbContext dbContext)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null ||
            !int.TryParse(httpContext.Items["chat_conversation_id"]?.ToString(), out var conversationId) ||
            !int.TryParse(httpContext.Items["chat_user_message_id"]?.ToString(), out var userMessageId) ||
            !int.TryParse(httpContext.Items["chat_assistant_sender_type_id"]?.ToString(), out var assistantSenderTypeId))
        {
            return false;
        }

        var latestUserMessage = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.Id == userMessageId && message.ConversationId == conversationId)
            .Select(message => message.Content)
            .FirstOrDefaultAsync();
        var previousAssistantMessage = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId &&
                              message.Id < userMessageId &&
                              message.SenderTypeId == assistantSenderTypeId)
            .OrderByDescending(message => message.Id)
            .Select(message => message.Content)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(latestUserMessage) || string.IsNullOrWhiteSpace(previousAssistantMessage))
        {
            return false;
        }

        bool assistantAskedToConfirm = Regex.IsMatch(
            previousAssistantMessage,
            @"\b(confirmas|confirma|confirmar|confirmación|confirmacion)\b[\s\S]{0,120}\b(cita|agendar|programar|reservar)\b|\b(cita|agendar|programar|reservar)\b[\s\S]{0,120}\b(confirmas|confirma|confirmar|confirmación|confirmacion)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        bool userDeclined = Regex.IsMatch(
            latestUserMessage,
            @"^\s*(no|mejor no|cancela|cancelar)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        bool userConfirmed = Regex.IsMatch(
            latestUserMessage,
            @"^\s*(sí|si|está bien|esta bien|de acuerdo|confirmo|confirmar|acepto|procede|adelante|crear|crea|genere|genera|haz|hazla|registra|registrar|agéndala|agendala)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return assistantAskedToConfirm &&
               HasCompleteAppointmentSummary(previousAssistantMessage) &&
               userConfirmed &&
               !userDeclined;
    }

    private static bool HasCompleteAppointmentSummary(string message)
    {
        var options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        bool hasPatient = Regex.IsMatch(message, @"\bpaciente\b", options);
        bool hasSite = Regex.IsMatch(message, @"\b(sede|ubicación|ubicacion)\b", options) &&
                       message.Contains("Oficentro Masaya", StringComparison.OrdinalIgnoreCase);
        bool hasDoctor = Regex.IsMatch(message, @"\b(doctor|m[eé]dico|medico)\b", options);
        bool hasReason = Regex.IsMatch(message, @"\bmotivo\b", options);
        bool hasDate = Regex.IsMatch(
            message,
            @"\b\d{4}-\d{2}-\d{2}\b|\b\d{1,2}\s+de\s+[a-záéíóúñ]+(?:\s+de\s+\d{4})?\b",
            options);
        bool hasTime = Regex.IsMatch(
            message,
            @"\b\d{1,2}:\d{2}\b|\b\d{1,2}(?::\d{2})?\s*(?:a\.?m\.?|p\.?m\.?|horas?|de la mañana|de la tarde|de la noche)\b",
            options);

        return hasPatient && hasSite && hasDoctor && hasReason && hasDate && hasTime;
    }

    [KernelFunction, Description("Busca citas futuras o de una fecha concreta por nombre del paciente o médico. Úsala antes de cancelar o reprogramar y solicita el ID exacto de la cita si hay más de una coincidencia.")]
    public async Task<string> BuscarCitas(string nombrePacienteOMedico, string? fecha = null)
    {
        if (!HasAnyView("Citas - Hoy", "Citas - Agendar", "Consultas - Activa", "Consultas - Historial"))
            return "Acceso denegado: No tienes permisos para consultar citas.";
        if (string.IsNullOrWhiteSpace(nombrePacienteOMedico))
            return "Error: Indica el nombre del paciente o del médico.";

        DateTime? dateFrom = null;
        DateTime? dateTo = null;
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            if (!DateTime.TryParseExact(fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return "Error: La fecha debe tener formato YYYY-MM-DD.";
            dateFrom = parsed;
            dateTo = parsed;
        }
        else
        {
            dateFrom = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetTimeZone.GetRequestTimeZone(_httpContextAccessor)).Date;
        }

        using var scope = _scopeFactory.CreateScope();
        var appointmentServices = scope.ServiceProvider.GetRequiredService<IAppointmentServices>();
        var result = await appointmentServices.GetListAsync(new AppointmentFilterDto
        {
            Search = nombrePacienteOMedico.Trim(), DateFrom = dateFrom, DateTo = dateTo
        });
        if (result.IsFailure || result.Value == null)
            return $"Error al consultar las citas: {result.Error?.Description ?? "Error de consulta."}";

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetTimeZone.GetRequestTimeZone(_httpContextAccessor));
        var items = result.Value.Where(a => (a.StatusId is 1 or 2) && a.StartTime > localNow)
            .OrderBy(a => a.StartTime).Take(20)
            .Select(a => new { a.Id, a.PatientFullName, a.DoctorFullName, Fecha = a.StartTime.ToString("yyyy-MM-dd HH:mm"), a.Status, a.Reason })
            .ToList();
        return items.Count == 0 ? "No se encontraron citas futuras activas con esos datos." : JsonSerializer.Serialize(items);
    }

    [KernelFunction, Description("Cancela o reprograma una cita existente después de confirmación explícita del usuario. Primero llama BuscarCitas y usa el ID exacto. Al reprogramar, usa fecha y hora local YYYY-MM-DDTHH:mm. Nunca afirmes éxito si la operación falla.")]
    public async Task<string> CambiarOCancelarCita(
        int citaId,
        [Description("Escribe cancelar o reprogramar.")] string accion,
        [Description("Nueva fecha y hora local YYYY-MM-DDTHH:mm; requerida para reprogramar.")] string? nuevaFechaHora = null)
    {
        if (!HasAnyView("Citas - Agendar", "Citas - Hoy", "Consultas - Activa"))
            return "Acceso denegado: No tienes permisos para cambiar citas.";
        if (citaId <= 0) return "Error: El número de cita no es válido.";

        using var scope = _scopeFactory.CreateScope();
        var appointmentServices = scope.ServiceProvider.GetRequiredService<IAppointmentServices>();
        var detail = await appointmentServices.GetById(citaId);
        if (detail.IsFailure || detail.Value == null)
            return $"Error: No se encontró la cita #{citaId}.";

        if (accion.Contains("cancel", StringComparison.OrdinalIgnoreCase))
        {
            var result = await appointmentServices.UpdateStatusAppointmentsAsync(new UpdateStatusAppointmenDto { AppointmenId = citaId, StatusId = 5 });
            if (result.IsFailure)
            {
                var escalationId = await CreateAppointmentEscalationAsync(scope, citaId,
                    $"No se pudo cancelar la cita por una regla del sistema: {result.Error?.Description ?? "operación rechazada"}.");
                return JsonSerializer.Serialize(new { exito = false, citaId, escalado = escalationId > 0, solicitudId = escalationId, mensaje = "La cancelación requiere gestión del personal." });
            }
            return
                JsonSerializer.Serialize(new { exito = true, citaId, estado = "Cancelada" });
        }

        if (!accion.Contains("reprogram", StringComparison.OrdinalIgnoreCase))
            return "Error: La acción debe ser cancelar o reprogramar.";
        if (!TryParseLocalAppointmentDate(nuevaFechaHora, out var newStart))
            return "Error: Para reprogramar indica fecha y hora en formato YYYY-MM-DDTHH:mm.";

        var update = await appointmentServices.Update(new AppointmentUpdateDto
        {
            Id = citaId,
            PatientId = detail.Value.Patient.Id,
            EmployeeId = detail.Value.DoctorId,
            StartTime = newStart,
            Duration = detail.Value.Duration,
            Reason = detail.Value.Reason,
            StatusId = 1
        }, detail.Value.Patient.Id);
        if (update.IsFailure)
        {
            var error = update.Error?.Description ?? string.Join(" ", update.Error?.ValidationErrors?.Select(e => e.ErrorMessage) ?? Array.Empty<string>());
            var escalationId = await CreateAppointmentEscalationAsync(scope, citaId,
                $"No se pudo reprogramar la cita a {newStart:yyyy-MM-dd HH:mm}; requiere revisión de disponibilidad. Detalle: {error}");
            return JsonSerializer.Serialize(new { exito = false, citaId, escalado = escalationId > 0, solicitudId = escalationId, mensaje = "El cambio requiere gestión del personal." });
        }

        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var reminder = await db.AppointmentReminders.FirstOrDefaultAsync(r => r.AppointmentId == citaId);
        if (reminder != null)
        {
            reminder.ScheduledAtUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(newStart, DateTimeKind.Unspecified), GetTimeZone.GetRequestTimeZone(_httpContextAccessor)).AddHours(-24);
            reminder.SentAtUtc = null;
            reminder.Attempts = 0;
            reminder.LastError = null;
            await db.SaveChangesAsync();
        }
        return JsonSerializer.Serialize(new { exito = true, citaId, fechaHora = newStart.ToString("yyyy-MM-dd HH:mm") });
    }

    private async Task<int> CreateAppointmentEscalationAsync(IServiceScope scope, int appointmentId, string summary)
    {
        var http = _httpContextAccessor.HttpContext;
        var userId = http?.Items.TryGetValue("chat_user_id", out var userValue) == true && userValue is int parsedUser
            ? parsedUser : 0;
        if (userId <= 0) return 0;
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var request = new ChatAppointmentEscalation
        {
            ConversationId = http?.Items.TryGetValue("chat_conversation_id", out var conversationValue) == true && conversationValue is int parsedConversation
                ? parsedConversation : null,
            CreatedByUserId = userId,
            AppointmentId = appointmentId,
            Summary = summary.Length > 1000 ? summary[..1000] : summary,
            Status = "pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ChatAppointmentEscalations.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }

    [KernelFunction, Description("Muestra cambios o cancelaciones de citas pendientes de revisión del personal.")]
    public async Task<string> ObtenerExcepcionesDeCitasPendientes()
    {
        if (!HasAnyView("Citas - Hoy", "Citas - Agendar", "Consultas - Activa"))
            return "Acceso denegado: No tienes permiso para revisar cambios de citas.";
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var list = await db.ChatAppointmentEscalations.AsNoTracking()
            .Where(e => e.Status == "pending")
            .OrderBy(e => e.CreatedAtUtc).Take(25)
            .Select(e => new { e.Id, e.AppointmentId, e.Summary, FechaUtc = e.CreatedAtUtc })
            .ToListAsync();
        return list.Count == 0 ? "No hay excepciones de citas pendientes." : JsonSerializer.Serialize(list);
    }

    [KernelFunction, Description("Marca como atendida una excepción de cita después de resolverla con el paciente.")]
    public async Task<string> ResolverExcepcionDeCita(int solicitudId)
    {
        if (!HasAnyView("Citas - Hoy", "Citas - Agendar"))
            return "Acceso denegado: No tienes permiso para cerrar excepciones de citas.";
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var item = await db.ChatAppointmentEscalations.FirstOrDefaultAsync(e => e.Id == solicitudId && e.Status == "pending");
        if (item == null) return "Error: No se encontró una excepción pendiente con ese número.";
        item.Status = "resolved";
        item.ResolvedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { exito = true, solicitudId, estado = "atendida" });
    }

    [KernelFunction, Description("Registra una solicitud de atención clínica para revisión del personal. No diagnostiques ni indiques tratamiento; resume solo lo que el usuario comunicó y define urgencia alta si describe síntomas graves.")]
    public async Task<string> SolicitarApoyoClinico(
        [Description("Resumen breve de la consulta o síntomas, sin añadir suposiciones médicas.")] string resumen,
        [Description("normal o alta; usa alta ante signos de alarma.")] string urgencia = "normal",
        int? pacienteId = null)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User.Identity?.IsAuthenticated != true)
            return "Acceso denegado: Debes iniciar sesión para solicitar apoyo clínico.";
        if (string.IsNullOrWhiteSpace(resumen)) return "Error: Describe brevemente la duda o situación.";

        var normalizedUrgency = urgencia.Equals("alta", StringComparison.OrdinalIgnoreCase) ? "alta" : "normal";
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var userId = http.Items.TryGetValue("chat_user_id", out var userValue) && userValue is int parsedUser
            ? parsedUser
            : 0;
        if (userId <= 0) return "Error: No se pudo asociar la solicitud al usuario.";

        var conversationId = http.Items.TryGetValue("chat_conversation_id", out var conversationValue) && conversationValue is int parsedConversation
            ? parsedConversation : (int?)null;
        if (pacienteId.HasValue && !await db.Patients.AnyAsync(p => p.Id == pacienteId.Value))
            return "Error: No se encontró el paciente indicado.";

        var escalation = new ChatClinicalEscalation
        {
            ConversationId = conversationId,
            CreatedByUserId = userId,
            PatientId = pacienteId,
            Summary = resumen.Trim().Length > 1000 ? resumen.Trim()[..1000] : resumen.Trim(),
            Urgency = normalizedUrgency,
            Status = "pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ChatClinicalEscalations.Add(escalation);
        await db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { exito = true, solicitudId = escalation.Id, urgencia = normalizedUrgency, mensaje = "La solicitud quedó registrada para revisión del personal clínico." });
    }

    [KernelFunction, Description("Muestra las solicitudes de apoyo clínico pendientes. Solo personal con acceso a consultas puede usarla.")]
    public async Task<string> ObtenerApoyosClinicosPendientes()
    {
        if (!HasAnyView("Consultas - Activa", "Consultas - Historial", "Citas - Hoy"))
            return "Acceso denegado: No tienes permiso para revisar solicitudes clínicas.";
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var list = await db.ChatClinicalEscalations.AsNoTracking()
            .Where(e => e.Status == "pending")
            .OrderByDescending(e => e.Urgency == "alta").ThenBy(e => e.CreatedAtUtc)
            .Take(25)
            .Select(e => new { e.Id, e.Urgency, e.Summary, e.PatientId, FechaUtc = e.CreatedAtUtc })
            .ToListAsync();
        return list.Count == 0 ? "No hay solicitudes clínicas pendientes." : JsonSerializer.Serialize(list);
    }

    [KernelFunction, Description("Marca como atendida una solicitud de apoyo clínico pendiente. Solo personal con acceso a consultas puede usarla.")]
    public async Task<string> ResolverApoyoClinico(int solicitudId)
    {
        if (!HasAnyView("Consultas - Activa", "Consultas - Historial"))
            return "Acceso denegado: No tienes permiso para cerrar solicitudes clínicas.";
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var item = await db.ChatClinicalEscalations.FirstOrDefaultAsync(e => e.Id == solicitudId && e.Status == "pending");
        if (item == null) return "Error: No se encontró una solicitud pendiente con ese número.";
        item.Status = "resolved";
        item.ResolvedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { exito = true, solicitudId, estado = "atendida" });
    }

    private static bool TryParseLocalAppointmentDate(string? value, out DateTime localDateTime)
    {
        var formats = new[] { "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd HH:mm" };
        if (DateTime.TryParseExact(value?.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out localDateTime))
        {
            localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            return true;
        }

        localDateTime = default;
        return false;
    }

    private static string TranslateAppointmentValidation(string message)
    {
        var normalized = message.Trim().ToLowerInvariant();
        return normalized switch
        {
            "the start time must be within the clinic's business hours." => "La hora está fuera del horario de atención de la clínica.",
            "the appointment must start and end within the clinic's business hours on the same day." => "La cita debe iniciar y terminar dentro del horario de atención de la clínica y el mismo día.",
            "appointments cannot be scheduled in the past." => "No se puede agendar una cita en el pasado.",
            "you must select a patient." => "Selecciona un paciente válido.",
            "you must select a doctor." => "Selecciona un médico válido.",
            "the duration should be between 5 and 150 minutes." => "La duración debe estar entre 5 y 150 minutos.",
            "the patient already has an appointment at that time." => "El paciente ya tiene una cita en ese horario.",
            "the doctor already has an appointment at that time." => "El médico ya tiene una cita en ese horario.",
            "the appointment must fit within the doctor's availability." => "El horario seleccionado no coincide con la disponibilidad del médico.",
            "the patient does not exist or is inactive" => "El paciente no existe o está inactivo.",
            "the doctor does not exist or is inactive." => "El médico no existe o está inactivo.",
            "unexpected error creating appointment." => "Ocurrió un error interno al guardar la cita.",
            _ => message
        };
    }

    [KernelFunction, Description("Busca pacientes por nombre o DNI, lista pacientes o consulta los más recientemente agregados. Para una solicitud completa de cita, extrae y busca el DNI asociado al paciente; no uses el DNI del médico. Cuando soliciten los últimos pacientes registrados, indica 'recientes' en terminoBusqueda.")]
    public async Task<string> BuscarPacientes(
        [Description("Nombre, apellido o DNI para buscar. Usa 'todos' para listar, o 'recientes'/'últimos agregados' para obtener los registros más nuevos primero.")] string? terminoBusqueda = null)
    {
        if (!HasAnyView(
                "Pacientes - Buscar", "Pacientes - Registrar", "Pacientes - Historial de Paciente",
                "Consultas - Activa", "Consultas - Crear", "Consultas - Historial", "Citas - Agendar"))
        {
            return "Acceso denegado: No tienes permisos para consultar pacientes.";
        }

        string? queryParam = null;
        bool recentSearch = false;
        if (!string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            string term = terminoBusqueda.Trim();
            var patientDni = ExtractDniForRole(term, @"\b(?:paciente|patient)\b") ?? ExtractDni(term);
            if (patientDni != null)
            {
                queryParam = patientDni;
            }
            else
            {
                string normalizedTerm = term.ToLowerInvariant();
                recentSearch = normalizedTerm.Contains("reciente") || normalizedTerm.Contains("último") ||
                               normalizedTerm.Contains("ultimo") || normalizedTerm.Contains("nuevo") ||
                               normalizedTerm.Contains("agregado") || normalizedTerm.Contains("agregados");

                bool allPatients = normalizedTerm.Equals("todos") || normalizedTerm.Equals("registrados");
                if (!recentSearch && !allPatients)
                {
                    queryParam = term;
                }
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var patientServices = scope.ServiceProvider.GetRequiredService<IPatientServices>();

        var pagination = new PaginationDto(20, 0)
        {
            Query = queryParam
        };

        var result = recentSearch
            ? await patientServices.GetRecentPatients(pagination)
            : await patientServices.SearchPatient(pagination);
        if (result.IsFailure || result.Value == null || result.Value.Items == null)
        {
            return $"No se encontraron pacientes: {result.Error?.Description ?? "Sin coincidencias."}";
        }

        var patients = result.Value.Items.Take(25).Select(p => new
        {
            p.Id,
            Paciente = p.FullName,
            FechaRegistro = p.CreatedAt
        }).ToList();

        if (!patients.Any())
        {
            return string.IsNullOrWhiteSpace(queryParam) 
                ? "No se encontraron pacientes registrados en el sistema." 
                : $"No se encontraron pacientes que coincidan con '{terminoBusqueda}'.";
        }

        return JsonSerializer.Serialize(patients);
    }

    [KernelFunction, Description("Obtiene los horarios generales de apertura y cierre de la clínica para los días de la semana.")]
    public async Task<string> ObtenerHorariosClinica()
    {
        if (!HasAnyView("Administración - Horarios", "Citas - Agendar", "Citas - Disponibilidad"))
        {
            return "Acceso denegado: No tienes permisos para consultar los horarios.";
        }

        using var scope = _scopeFactory.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var result = await scheduleService.GetClinicSchedulesAsync();
        if (result.IsFailure || result.Value == null)
        {
            return $"No se pudieron obtener los horarios de la clínica: {result.Error?.Description ?? "Error de consulta."}";
        }

        var schedules = result.Value.Take(25).Select(s => new
        {
            Dia = s.DayName,
            Estado = s.IsOpen ? "Abierto" : "Cerrado",
            Apertura = s.OpenTime,
            Cierre = s.CloseTime
        }).ToList();

        return JsonSerializer.Serialize(schedules);
    }
}
