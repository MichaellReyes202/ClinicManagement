using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatAiModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();
}
