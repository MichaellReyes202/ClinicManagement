using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Chat;
using Application.Interfaces;
using Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ChatController(
        IChatService chatService,
        ILogger<ChatController> logger,
        IWebHostEnvironment environment)
    {
        _chatService = chatService;
        _logger = logger;
        _environment = environment;
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

        if (string.IsNullOrWhiteSpace(dto.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await foreach (var streamEvent in _chatService.SendMessageStreamAsync(userId, roleId, dto, ct))
            {
                await WriteStreamEventAsync(streamEvent, ct);
                await Response.Body.FlushAsync(ct);

                if (streamEvent.Type == "error")
                {
                    return;
                }
            }

            await WriteStreamEventAsync(new ChatStreamEventDto { Type = "done" }, ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // El cliente desconectó la sesión SSE
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando streaming de chat para el usuario {UserId}", userId);
            if (!ct.IsCancellationRequested)
            {
                var rootException = ex.GetBaseException();
                await WriteStreamEventAsync(new ChatStreamEventDto
                {
                    Type = "error",
                    Message = _environment.IsDevelopment()
                        ? $"Error interno ({rootException.GetType().Name}): {rootException.Message}"
                        : "No se pudo completar la respuesta. Intenta nuevamente."
                }, CancellationToken.None);
                await Response.Body.FlushAsync(CancellationToken.None);
            }
        }
    }

    private Task WriteStreamEventAsync(ChatStreamEventDto streamEvent, CancellationToken ct)
    {
        var data = System.Text.Json.JsonSerializer.Serialize(
            streamEvent,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return Response.WriteAsync($"data: {data}\n\n", ct);
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
