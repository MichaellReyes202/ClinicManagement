using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Chat;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(claim, out int userId))
        {
            return userId;
        }

        return 0;
    }

    private int GetCurrentRoleId()
    {
        var claim = User.FindFirst("roleId")?.Value;
        if (int.TryParse(claim, out int roleId))
        {
            return roleId;
        }
        return 0;
    }

    /// <summary>
    /// Enviar un mensaje al asistente con respuesta en Server-Sent Events (SSE) streaming.
    /// </summary>
    [HttpPost("stream")]
    [EnableRateLimiting("ChatStreamPolicy")]
    [Produces("text/event-stream")]
    public async Task SendMessageStream([FromBody] ChatSendMessageDto dto, CancellationToken ct)
    {
        int userId = GetCurrentUserId();
        int roleId = GetCurrentRoleId();

        if (userId <= 0)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await foreach (var chunk in _chatService.SendMessageStreamAsync(userId, roleId, dto, ct))
            {
                var formattedData = chunk.Replace("\n", "\\n");
                await Response.WriteAsync($"data: {formattedData}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // El cliente desconectó la sesión SSE
        }
        catch (Exception ex)
        {
            // Captura errores del modelo (ej. Ollama fuera de línea, HTTP 500/404 de IA, error de BD)
            // y envía un evento SSE limpio en lugar de romper abruptamente la conexión TCP
            var safeMessage = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = $"Error en el asistente de IA: {ex.Message}"
            });
            await Response.WriteAsync($"data: {safeMessage}\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Listar conversaciones del usuario autenticado de forma paginada.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] PaginationDto pagination,
        [FromQuery] bool includeArchived = false)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.GetConversationsAsync(userId, pagination, includeArchived);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Obtener detalle y mensajes de una conversación específica.
    /// </summary>
    [HttpGet("conversations/{id:int}")]
    public async Task<IActionResult> GetConversationDetail(int id)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.GetConversationDetailAsync(userId, id);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Renombrar el título de una conversación.
    /// </summary>
    [HttpPut("conversations/{id:int}/title")]
    public async Task<IActionResult> RenameConversation(int id, [FromBody] ChatRenameDto dto)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.RenameConversationAsync(userId, id, dto.Title);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>
    /// Anclar o desanclar una conversación.
    /// </summary>
    [HttpPatch("conversations/{id:int}/pin")]
    public async Task<IActionResult> TogglePin(int id)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.TogglePinAsync(userId, id);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>
    /// Archivar o desarchivar una conversación.
    /// </summary>
    [HttpPatch("conversations/{id:int}/archive")]
    public async Task<IActionResult> ArchiveConversation(int id)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.ArchiveConversationAsync(userId, id);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>
    /// Eliminar una conversación.
    /// </summary>
    [HttpDelete("conversations/{id:int}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.DeleteConversationAsync(userId, id);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>
    /// Registrar o actualizar el feedback de un mensaje (positivo/negativo).
    /// </summary>
    [HttpPost("feedback")]
    public async Task<IActionResult> AddFeedback([FromBody] ChatFeedbackDto dto)
    {
        int userId = GetCurrentUserId();
        var result = await _chatService.AddFeedbackAsync(userId, dto);
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
