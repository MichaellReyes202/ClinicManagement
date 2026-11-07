using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ChatClinicalEscalation
{
    public int Id { get; set; }

    public int? ConversationId { get; set; }

    public int CreatedByUserId { get; set; }

    public int? PatientId { get; set; }

    public string Summary { get; set; } = null!;

    public string Urgency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public virtual ChatConversation? Conversation { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Patient? Patient { get; set; }
}
