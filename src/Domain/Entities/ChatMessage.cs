using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public int SenderTypeId { get; set; }

    public string Content { get; set; } = null!;

    public int? TokensUsed { get; set; }

    public int? ExecutionTimeMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatFeedback> ChatFeedbacks { get; set; } = new List<ChatFeedback>();

    public virtual ICollection<ChatToolExecution> ChatToolExecutions { get; set; } = new List<ChatToolExecution>();

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual CatChatSenderType SenderType { get; set; } = null!;
}
