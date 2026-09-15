using System;

namespace Application.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public required string SenderType { get; set; }
    public required string Content { get; set; }
    public int? TokensUsed { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
