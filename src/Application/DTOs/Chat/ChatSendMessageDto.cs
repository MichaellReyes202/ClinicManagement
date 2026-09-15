namespace Application.DTOs.Chat;

public class ChatSendMessageDto
{
    public int? ConversationId { get; set; }
    public required string Message { get; set; }
}
