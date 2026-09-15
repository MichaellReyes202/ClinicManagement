using System;
using System.Collections.Generic;

namespace Application.Interfaces;

public class ChatToolExecutionRecord
{
    public required string ToolName { get; set; }
    public string? ToolInputJson { get; set; }
    public string? ToolOutputJson { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IChatToolExecutionTracker
{
    void AddExecution(ChatToolExecutionRecord record);
    IReadOnlyList<ChatToolExecutionRecord> GetExecutions();
    void Clear();
}
