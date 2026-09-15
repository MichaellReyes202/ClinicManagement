using System.Collections.Generic;

namespace Application.DTOs.Chat;

public class ChatConversationDetailDto : ChatConversationDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
}
