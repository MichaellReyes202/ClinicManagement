using System.Collections.Generic;
using Application.Interfaces;

namespace Infrastructure.Services;

public class ChatToolExecutionTracker : IChatToolExecutionTracker
{
    private readonly List<ChatToolExecutionRecord> _records = new();

    public void AddExecution(ChatToolExecutionRecord record)
    {
        lock (_records)
        {
            _records.Add(record);
        }
    }

    public IReadOnlyList<ChatToolExecutionRecord> GetExecutions()
    {
        lock (_records)
        {
            return _records.ToArray();
        }
    }

    public void Clear()
    {
        lock (_records)
        {
            _records.Clear();
        }
    }
}
