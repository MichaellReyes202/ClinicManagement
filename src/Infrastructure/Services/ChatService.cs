using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Chat;
using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly ClinicDbContext _dbContext;
    private readonly AiSettings _aiSettings;
    private readonly IChatToolExecutionTracker _executionTracker;

    public ChatService(
        Kernel kernel,
        ClinicDbContext dbContext,
        IOptions<AiSettings> aiSettings,
        IChatToolExecutionTracker executionTracker)
    {
        _kernel = kernel;
        _dbContext = dbContext;
        _aiSettings = aiSettings.Value;
        _executionTracker = executionTracker;
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        int userId,
        int roleId,
        ChatSendMessageDto dto,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Obtener modelo de IA activo desde cat_ai_models
        var activeModel = await _dbContext.CatAiModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsActive, ct);

        // 2. Obtener tipos de remitente (1=Usuario, 2=Asistente, 3=Sistema)
        var senderTypes = await _dbContext.CatChatSenderTypes
            .AsNoTracking()
            .ToListAsync(ct);

        int userSenderTypeId = senderTypes.FirstOrDefault(s => 
            s.Name.Equals("Usuario", StringComparison.OrdinalIgnoreCase) || 
            s.Name.Equals("User", StringComparison.OrdinalIgnoreCase))?.Id ?? 1;

        int assistantSenderTypeId = senderTypes.FirstOrDefault(s => 
            s.Name.Equals("Asistente", StringComparison.OrdinalIgnoreCase) || 
            s.Name.Equals("Assistant", StringComparison.OrdinalIgnoreCase))?.Id ?? 2;

        // 3. Buscar o crear la conversación
        ChatConversation? conversation = null;
        if (dto.ConversationId.HasValue && dto.ConversationId.Value > 0)
        {
            conversation = await _dbContext.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == dto.ConversationId.Value && c.UserId == userId, ct);
        }

        if (conversation == null)
        {
            // Título generado automáticamente a partir de los primeros 50 caracteres del primer mensaje (sin LLM)
            string title = dto.Message.Trim().Length > 50 ? dto.Message.Trim()[..47] + "..." : dto.Message.Trim();
            conversation = new ChatConversation
            {
                UserId = userId,
                RoleId = roleId > 0 ? roleId : null,
                ModelId = activeModel?.Id,
                Title = title,
                IsPinned = false,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ChatConversations.Add(conversation);
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            if (activeModel != null && conversation.ModelId != activeModel.Id)
            {
                conversation.ModelId = activeModel.Id;
            }
            await _dbContext.SaveChangesAsync(ct);
        }

        // 4. Guardar mensaje del usuario
        var userMsg = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderTypeId = userSenderTypeId,
            Content = dto.Message,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(userMsg);
        await _dbContext.SaveChangesAsync(ct);

        // 5. Cargar información de usuario, rol y fecha para el System Prompt dinámico
        var userEntity = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.EmployeeUser)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        string userName = userEntity?.EmployeeUser != null 
            ? $"{userEntity.EmployeeUser.FirstName} {userEntity.EmployeeUser.LastName}".Trim() 
            : userEntity?.Email ?? "Usuario";

        string roleName = "Usuario";
        if (roleId > 0)
        {
            var role = await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, ct);
            if (role != null && !string.IsNullOrWhiteSpace(role.Name))
            {
                roleName = role.Name;
            }
        }

        string currentDateTimeStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                string systemPrompt = $@"Eres un asistente virtual especializado exclusivamente en la gestión clínica de la institución médica.

REGLAS OBLIGATORIAS:
1. ÁMBITO: Solo debes responder preguntas sobre el ámbito clínico y de gestión médica de la aplicación (citas, pacientes, consultas, exámenes, medicamentos, recetas, empleados y horarios de la clínica). Si el usuario solicita información sobre temas ajenos, declina amablemente.
2. CAPACIDAD Y FORMATO DE TABLAS: TIENES TOTAL CAPACIDAD de estructurar la información en tablas utilizando sintaxis Markdown de GitHub (ejemplo: `| Nombre | DNI | Teléfono |`). Cuando el usuario solicite una lista, tabla o resumen, NUNCA digas que no puedes generar tablas; consulta la información necesaria mediante tus herramientas e imprímela directamente en una tabla Markdown clara y limpia.
3. INVOCACIÓN DE HERRAMIENTAS: Utiliza las herramientas disponibles para consultar la base de datos antes de responder. NUNCA imprimas fragmentos JSON o etiquetas de invocación internas como `<tool_call>` o `brtc` en tu respuesta final.

INFORMACIÓN DEL CONTEXTO ACTUAL:
- Nombre del usuario: {userName}
- Rol del usuario: {roleName}
- Fecha y hora actual: {currentDateTimeStr}

Sé profesional, conciso y preciso en todas tus respuestas adaptadas a las responsabilidades del rol de '{roleName}'.";

        // 6. Cargar la ventana de contexto (5 mensajes desde configuración)
        int windowSize = _aiSettings.ContextWindowSize > 0 ? _aiSettings.ContextWindowSize : 5;

        var pastMessages = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id 
                     && m.Id != userMsg.Id 
                     && (m.SenderTypeId == userSenderTypeId || m.SenderTypeId == assistantSenderTypeId))
            .OrderByDescending(m => m.Id)
            .Take(windowSize)
            .Select(m => new 
            { 
                m.Id, 
                m.SenderTypeId, 
                m.Content 
            })
            .ToListAsync(ct);

        pastMessages.Reverse();

        // Si tras el recorte el historial empieza con un mensaje del assistant, descartarlo
        if (pastMessages.Count > 0 && pastMessages[0].SenderTypeId == assistantSenderTypeId)
        {
            pastMessages.RemoveAt(0);
        }

        var chatHistory = new ChatHistory(systemPrompt);

        foreach (var msg in pastMessages)
        {
            if (msg.SenderTypeId == userSenderTypeId)
            {
                chatHistory.AddUserMessage(msg.Content);
            }
            else if (msg.SenderTypeId == assistantSenderTypeId)
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        chatHistory.AddUserMessage(dto.Message);

        // 7. Ejecutar streaming con Semantic Kernel
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var sw = Stopwatch.StartNew();
        var assistantContentBuilder = new StringBuilder();
        int? tokensUsed = null;

        var streamingResponse = chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory: chatHistory,
            executionSettings: executionSettings,
            kernel: _kernel,
            cancellationToken: ct);

        try
        {
            await foreach (var chunk in streamingResponse.WithCancellation(ct))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    // Evitar transmitir marcas o JSONs crudos de invocación de herramientas
                    if (IsToolCallMarkup(chunk.Content))
                    {
                        continue;
                    }

                    assistantContentBuilder.Append(chunk.Content);
                    yield return chunk.Content;
                }

                if (tokensUsed == null && chunk.Metadata != null)
                {
                    if (chunk.Metadata.TryGetValue("Usage", out var usageObj) && usageObj != null)
                    {
                        if (usageObj is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            if (elem.TryGetProperty("total_tokens", out var tt)) tokensUsed = tt.GetInt32();
                            else if (elem.TryGetProperty("TotalTokens", out var tt2)) tokensUsed = tt2.GetInt32();
                        }
                        else
                        {
                            var prop = usageObj.GetType().GetProperty("TotalTokens") ?? usageObj.GetType().GetProperty("total_tokens");
                            if (prop != null && prop.GetValue(usageObj) is int propVal)
                            {
                                tokensUsed = propVal;
                            }
                        }
                    }
                    else if (chunk.Metadata.TryGetValue("TotalTokens", out var ttDirect) && ttDirect is int ttInt)
                    {
                        tokensUsed = ttInt;
                    }
                    else if (chunk.Metadata.TryGetValue("eval_count", out var evalCountObj) && evalCountObj is int evalCount)
                    {
                        tokensUsed = evalCount;
                    }
                }
            }
        }
        finally
        {
            sw.Stop();

            string cleanedContent = CleanAssistantContent(assistantContentBuilder.ToString());

            // 8. Persistir la respuesta del asistente (incluso si la petición fue cancelada)
            if (cleanedContent.Length > 0)
            {
                var assistantMsg = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    SenderTypeId = assistantSenderTypeId,
                    Content = cleanedContent,
                    TokensUsed = tokensUsed,
                    ExecutionTimeMs = (int)sw.ElapsedMilliseconds,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ChatMessages.Add(assistantMsg);
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                // 9. Persistir ejecuciones de herramientas acumuladas asociadas a este mensaje
                var toolExecutions = _executionTracker.GetExecutions();
                if (toolExecutions.Count > 0)
                {
                    foreach (var exec in toolExecutions)
                    {
                        _dbContext.ChatToolExecutions.Add(new ChatToolExecution
                        {
                            MessageId = assistantMsg.Id,
                            ToolName = exec.ToolName,
                            ToolInputJson = exec.ToolInputJson,
                            ToolOutputJson = exec.ToolOutputJson,
                            IsSuccess = exec.IsSuccess,
                            ErrorMessage = exec.ErrorMessage,
                            CreatedAt = exec.CreatedAt
                        });
                    }
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    _executionTracker.Clear();
                }
            }
        }
    }

    public async Task<Result<PaginatedResponseDto<ChatConversationDto>>> GetConversationsAsync(
        int userId,
        PaginationDto pagination,
        bool includeArchived = false)
    {
        var query = _dbContext.ChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && (includeArchived || !c.IsArchived));

        if (!string.IsNullOrWhiteSpace(pagination.Query))
        {
            string q = pagination.Query.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(q));
        }

        query = query.OrderByDescending(c => c.IsPinned).ThenByDescending(c => c.UpdatedAt);

        int totalRecords = await query.CountAsync();

        var items = await query
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .Select(c => new ChatConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                RoleName = c.Role != null ? c.Role.Name : null,
                ModelName = c.Model != null ? c.Model.Name : null,
                IsPinned = c.IsPinned,
                IsArchived = c.IsArchived,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        var paginatedResult = new PaginatedResponseDto<ChatConversationDto>(totalRecords, items, pagination.Limit);
        return Result<PaginatedResponseDto<ChatConversationDto>>.Success(paginatedResult);
    }

    public async Task<Result<ChatConversationDetailDto>> GetConversationDetailAsync(int userId, int conversationId)
    {
        var conv = await _dbContext.ChatConversations
            .AsNoTracking()
            .Include(c => c.Role)
            .Include(c => c.Model)
            .Include(c => c.ChatMessages)
                .ThenInclude(m => m.SenderType)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conv == null)
        {
            return Result<ChatConversationDetailDto>.Failure(
                new Error(ErrorCodes.NotFound, "La conversación no existe o no le pertenece."));
        }

        var detail = new ChatConversationDetailDto
        {
            Id = conv.Id,
            Title = conv.Title,
            RoleName = conv.Role?.Name,
            ModelName = conv.Model?.Name,
            IsPinned = conv.IsPinned,
            IsArchived = conv.IsArchived,
            CreatedAt = conv.CreatedAt,
            UpdatedAt = conv.UpdatedAt,
            Messages = conv.ChatMessages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderType = m.SenderType.Name,
                    Content = m.Content,
                    TokensUsed = m.TokensUsed,
                    ExecutionTimeMs = m.ExecutionTimeMs,
                    CreatedAt = m.CreatedAt
                })
                .ToList()
        };

        return Result<ChatConversationDetailDto>.Success(detail);
    }

    public async Task<Result> RenameConversationAsync(int userId, int conversationId, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            return Result.Failure(new Error(ErrorCodes.BadRequest, "El título de la conversación no puede estar vacío."));
        }

        var conv = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conv == null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, "La conversación no existe o no le pertenece."));
        }

        conv.Title = newTitle.Trim();
        conv.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> TogglePinAsync(int userId, int conversationId)
    {
        var conv = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conv == null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, "La conversación no existe o no le pertenece."));
        }

        conv.IsPinned = !conv.IsPinned;
        conv.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ArchiveConversationAsync(int userId, int conversationId)
    {
        var conv = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conv == null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, "La conversación no existe o no le pertenece."));
        }

        conv.IsArchived = !conv.IsArchived;
        conv.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteConversationAsync(int userId, int conversationId)
    {
        var conv = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conv == null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, "La conversación no existe o no le pertenece."));
        }

        _dbContext.ChatConversations.Remove(conv);
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AddFeedbackAsync(int userId, ChatFeedbackDto dto)
    {
        var msg = await _dbContext.ChatMessages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == dto.MessageId && m.Conversation.UserId == userId);

        if (msg == null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, "El mensaje no fue encontrado o no le pertenece."));
        }

        var existingFeedback = await _dbContext.ChatFeedbacks
            .FirstOrDefaultAsync(f => f.MessageId == dto.MessageId && f.UserId == userId);

        if (existingFeedback != null)
        {
            existingFeedback.IsPositive = dto.IsPositive;
            existingFeedback.Comment = dto.Comment;
        }
        else
        {
            _dbContext.ChatFeedbacks.Add(new ChatFeedback
            {
                MessageId = dto.MessageId,
                UserId = userId,
                IsPositive = dto.IsPositive,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }

    private static bool IsToolCallMarkup(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string t = text.Trim();
        return t.StartsWith("<tool_call>", StringComparison.OrdinalIgnoreCase) ||
               t.StartsWith("</tool_call>", StringComparison.OrdinalIgnoreCase) ||
               t.StartsWith("brtc", StringComparison.OrdinalIgnoreCase) ||
               t.StartsWith("[TOOL_CALLS]", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("ClinicPlugin-") ||
               t.Contains("\"name\":");
    }

    private static string CleanAssistantContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        string cleaned = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"<tool_call>.*?</tool_call>",
            "",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"brtc\s*\{.*?\}(\s*</tool_call>)?",
            "",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\{\s*""name""\s*:\s*""ClinicPlugin-[^}]+\}",
            "",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return cleaned.Trim();
    }
}
