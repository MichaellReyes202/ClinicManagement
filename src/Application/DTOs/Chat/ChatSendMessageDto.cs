using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Chat;

public class ChatSendMessageDto
{
    public int? ConversationId { get; set; }
    [Required, StringLength(4000, MinimumLength = 1)]
    public required string Message { get; set; }
}
