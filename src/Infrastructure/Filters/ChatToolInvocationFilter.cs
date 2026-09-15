using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        string? inputJson = null;
        try
        {
            if (context.Arguments != null && context.Arguments.Count > 0)
            {
                var dict = context.Arguments.ToDictionary(k => k.Key, v => v.Value);
                inputJson = JsonSerializer.Serialize(dict);
            }
        }
        catch
        {
            inputJson = "{}";
        }

        bool isSuccess = true;
        string? errorMessage = null;
        string? outputJson = null;
        DateTime createdAt = DateTime.UtcNow;

        try
        {
            await next(context);

            var resultValue = context.Result.GetValue<object>();
            if (resultValue != null)
            {
                string rawOutput = resultValue.ToString() ?? string.Empty;

                if (rawOutput.StartsWith("Acceso denegado", StringComparison.OrdinalIgnoreCase) ||
                    rawOutput.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                {
                    isSuccess = false;
                    errorMessage = rawOutput;
                }

                outputJson = SanitizeOutput(rawOutput);
            }
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            outputJson = JsonSerializer.Serialize(new { error = ex.Message });
        }

        // Obtener el tracker acumulador de la petición HTTP actual (Scoped)
        var tracker = _httpContextAccessor.HttpContext?.RequestServices.GetService<IChatToolExecutionTracker>();
        if (tracker != null)
        {
            // Acumular la ejecución en memoria (NO escribir en BD en el filtro)
            tracker.AddExecution(new ChatToolExecutionRecord
            {
                ToolName = fullName,
                ToolInputJson = inputJson,
                ToolOutputJson = outputJson,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CreatedAt = createdAt
            });
        }
    }

    private static string SanitizeOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return rawOutput;
        }

        // Enmascarar números de DNI (formatos tipo 0801-1990-12345 o secuencias de 13 dígitos)
        string sanitized = Regex.Replace(
            rawOutput,
            @"\b(\d{4})[-]?(\d{4})[-]?(\d{5})\b",
            "$1-$2-*****");

        // Recortar salidas excesivamente largas para logs
        if (sanitized.Length > 4000)
        {
            sanitized = sanitized[..3997] + "...";
        }

        return sanitized;
    }
}
