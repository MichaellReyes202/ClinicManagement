namespace Application.DTOs.Chat;

public class ChatFeedbackDto
{
    public int MessageId { get; set; }
    public bool IsPositive { get; set; }
    public string? Comment { get; set; }
}
