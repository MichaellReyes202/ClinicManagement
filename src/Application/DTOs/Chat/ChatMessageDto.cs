using System;
using System.Collections.Generic;

namespace Application.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public required string SenderType { get; set; }
    public required string Content { get; set; }
    public int? TokensUsed { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public List<string> ToolNames { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
