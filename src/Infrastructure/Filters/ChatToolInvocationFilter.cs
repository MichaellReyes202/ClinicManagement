using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Infrastructure.Filters;

public class ChatToolInvocationFilter : IFunctionInvocationFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatToolInvocationFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        string pluginName = context.Function.PluginName ?? string.Empty;
        string functionName = context.Function.Name ?? string.Empty;
        string fullName = string.IsNullOrWhiteSpace(pluginName) ? functionName : $"{pluginName}.{functionName}";

        bool isSuccess = true;
        string? errorMessage = null;
        Exception? invocationException = null;

        try
        {
            await next(context);

            var resultValue = context.Result.GetValue<object>();
            if (resultValue is string rawOutput &&
                (rawOutput.StartsWith("Acceso denegado", StringComparison.OrdinalIgnoreCase) ||
                 rawOutput.StartsWith("Error", StringComparison.OrdinalIgnoreCase)))
            {
                isSuccess = false;
                errorMessage = "La herramienta devolvió un error.";
            }
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = "La herramienta falló durante su ejecución.";
            invocationException = ex;
        }

        // Obtener el tracker acumulador de la petición HTTP actual (Scoped)
        var tracker = _httpContextAccessor.HttpContext?.RequestServices.GetService<IChatToolExecutionTracker>();
        if (tracker != null)
        {
            // Acumular la ejecución en memoria (NO escribir en BD en el filtro)
            tracker.AddExecution(new ChatToolExecutionRecord
            {
                ToolName = fullName,
                // No almacenar argumentos ni resultados: pueden incluir DNI, nombres,
                // teléfonos u otros datos de salud incluidos en la consulta.
                ToolInputJson = "{\"redacted\":true}",
                ToolOutputJson = "{\"redacted\":true}",
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (invocationException != null)
        {
            ExceptionDispatchInfo.Capture(invocationException).Throw();
        }
    }
}
