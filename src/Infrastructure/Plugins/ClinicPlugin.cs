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

    private bool IsAuthenticated()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true;
    }

    [KernelFunction, Description("Obtiene la lista de citas médicas agendadas para el día de hoy o para una fecha específica. Permite al personal y doctores consultar la agenda.")]
    public async Task<string> ObtenerCitasDelDia(
        [Description("Fecha a consultar en formato YYYY-MM-DD (ejemplo: 2026-09-14). Opcional, si no se especifica se consulta la fecha actual.")] string? fecha = null)
    {
        if (!IsAuthenticated())
        {
            return "Acceso denegado: El usuario debe estar autenticado en la plataforma clínica para consultar la agenda de citas.";
        }

        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            if (DateTime.TryParseExact(fecha.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                parsedDate = dt;
            }
            else if (DateTime.TryParse(fecha, out var dtAny))
            {
                parsedDate = dtAny;
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

    [KernelFunction, Description("Busca pacientes registrados en el sistema de la clínica por su nombre, apellido o número de DNI.")]
    public async Task<string> BuscarPacientes(
        [Description("Nombre, apellido o número de DNI del paciente a buscar.")] string terminoBusqueda)
    {
        if (!IsAuthenticated())
        {
            return "Acceso denegado: El usuario debe estar autenticado para buscar pacientes.";
        }

        if (string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            return "Debe ingresar un término de búsqueda válido (nombre, apellido o DNI).";
        }

        using var scope = _scopeFactory.CreateScope();
        var patientServices = scope.ServiceProvider.GetRequiredService<IPatientServices>();

        var pagination = new PaginationDto(20, 0)
        {
            Query = terminoBusqueda.Trim()
        };

        var result = await patientServices.SearchPatient(pagination);
        if (result.IsFailure || result.Value == null || result.Value.Items == null)
        {
            return $"No se encontraron pacientes: {result.Error?.Description ?? "Sin coincidencias."}";
        }

        var patients = result.Value.Items.Take(25).Select(p => new
        {
            p.Id,
            Paciente = p.FullName,
            p.Dni,
            Telefono = p.ContactPhone
        }).ToList();

        if (!patients.Any())
        {
            return $"No se encontraron pacientes que coincidan con '{terminoBusqueda}'.";
        }

        return JsonSerializer.Serialize(patients);
    }

    [KernelFunction, Description("Obtiene los horarios generales de apertura y cierre de la clínica para los días de la semana.")]
    public async Task<string> ObtenerHorariosClinica()
    {
        if (!IsAuthenticated())
        {
            return "Acceso denegado: Se requiere una sesión activa en la clínica.";
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
