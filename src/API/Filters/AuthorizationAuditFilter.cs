


using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace API.Filters;
public class AuthorizationAuditFilter : IAsyncAuthorizationFilter
{
    private readonly IAuditlogServices _auditlogServices;
    private readonly IUserService _userService;

    public AuthorizationAuditFilter(IAuditlogServices auditlogServices , IUserService userService)
    {
        _auditlogServices = auditlogServices;
        _userService = userService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Result is ForbidResult || context.Result is StatusCodeResult { StatusCode: 403 })
        {
            var user = context.HttpContext.User;
            var endpoint = context.ActionDescriptor.DisplayName ?? context.ActionDescriptor.RouteValues["controller"];
            int? userId = null;
            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out int id))
                {
                    userId = id;
                }
            }
            var requiredRoles = context.ActionDescriptor.EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .Where(attr => !string.IsNullOrEmpty(attr.Roles))
                .SelectMany(attr => attr.Roles!.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .ToList();

            string detailMessage;
            if (requiredRoles.Any())
            {
                detailMessage = $"Intento de acceso denegado. Roles requeridos: {string.Join(", ", requiredRoles)}. Usuario: {user.Identity?.Name ?? "Anónimo"}.";
            }
            else
            {
                detailMessage = $"Intento de acceso denegado por política de seguridad general. Usuario: {user.Identity?.Name ?? "Anónimo"}.";
            }

            // 3. Registrar el Evento de Auditoría
            try
            {
                // La llamada ahora incluye el parámetro changeDetail, resolviendo el error CS7036.
                await _auditlogServices.RegisterAuthorizationFailureAsync(
                    userId: userId,
                    module: AuditModuletype.System, // Evento de seguridad
                    actionType: ActionType.AUTH_DENIED, // Usando el Enum
                    recordDisplay: $"Acceso a {endpoint}",
                    status: AuditStatus.WARNING,
                    changeDetail: detailMessage // ¡Parámetro requerido añadido!
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar auditoría de AUTH_DENIED: {ex.Message}");
            }
        }

        // La ejecución del pipeline debe continuar para enviar el 403 al cliente.
        await Task.CompletedTask;
    }
}
