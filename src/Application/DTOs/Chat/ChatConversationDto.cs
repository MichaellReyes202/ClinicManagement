using System;

namespace Application.DTOs.Chat;

public class ChatConversationDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? RoleName { get; set; }
    public string? ModelName { get; set; }
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
