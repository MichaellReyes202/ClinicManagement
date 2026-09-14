using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CatChatSenderType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
