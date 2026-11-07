namespace Domain.Entities;

public class ChatAppointmentEscalation
{
    public int Id { get; set; }
    public int? ConversationId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AppointmentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public virtual ChatConversation? Conversation { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual Appointment? Appointment { get; set; }
}
