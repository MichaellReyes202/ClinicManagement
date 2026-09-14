using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ChatToolExecution
{
    public int Id { get; set; }

    public int MessageId { get; set; }

    public string ToolName { get; set; } = null!;

    public string? ToolInputJson { get; set; }

    public string? ToolOutputJson { get; set; }

    public bool IsSuccess { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatMessage Message { get; set; } = null!;
}
