namespace Application.DTOs.Chat;

public class ChatStreamEventDto
{
    public required string Type { get; set; }
    public int? ConversationId { get; set; }
    public string? Content { get; set; }
    public string? Message { get; set; }
}
