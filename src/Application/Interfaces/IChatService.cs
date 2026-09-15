using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.DTOs.Chat;
using Domain.Errors;

namespace Application.Interfaces;

public interface IChatService
{
    IAsyncEnumerable<string> SendMessageStreamAsync(int userId, int roleId, ChatSendMessageDto dto, CancellationToken ct = default);
    Task<Result<PaginatedResponseDto<ChatConversationDto>>> GetConversationsAsync(int userId, PaginationDto pagination, bool includeArchived = false);
    Task<Result<ChatConversationDetailDto>> GetConversationDetailAsync(int userId, int conversationId);
    Task<Result> RenameConversationAsync(int userId, int conversationId, string newTitle);
    Task<Result> TogglePinAsync(int userId, int conversationId);
    Task<Result> ArchiveConversationAsync(int userId, int conversationId);
    Task<Result> DeleteConversationAsync(int userId, int conversationId);
    Task<Result> AddFeedbackAsync(int userId, ChatFeedbackDto dto);
}
