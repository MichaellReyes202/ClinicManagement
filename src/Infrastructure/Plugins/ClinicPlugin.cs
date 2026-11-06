using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Appointment;
using Application.DTOs.Patient;
using Application.DTOs.Schedule;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
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

    [KernelFunction, Description("Busca pacientes por nombre o DNI, lista pacientes o consulta los más recientemente agregados. Cuando soliciten los últimos pacientes registrados, indica 'recientes' en terminoBusqueda; los resultados se ordenan por fecha de registro descendente.")]
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
            string term = terminoBusqueda.Trim().ToLowerInvariant();
            recentSearch = term.Contains("reciente") || term.Contains("último") ||
                           term.Contains("ultimo") || term.Contains("nuevo") ||
                           term.Contains("agregado") || term.Contains("agregados");

            bool allPatients = term.Equals("todos") || term.Equals("registrados");
            if (!recentSearch && !allPatients)
            {
                queryParam = terminoBusqueda.Trim();
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
