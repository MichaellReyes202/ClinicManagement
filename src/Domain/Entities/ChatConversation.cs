using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ChatConversation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? RoleId { get; set; }

    public int? ModelId { get; set; }

    public string Title { get; set; } = null!;

    public bool IsPinned { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual CatAiModel? Model { get; set; }

    public virtual Role? Role { get; set; }

    public virtual User User { get; set; } = null!;
}
