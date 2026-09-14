using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ChatFeedback
{
    public int Id { get; set; }

    public int MessageId { get; set; }

    public int UserId { get; set; }

    public bool IsPositive { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatMessage Message { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
