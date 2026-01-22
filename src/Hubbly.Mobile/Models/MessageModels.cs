namespace Hubbly.Mobile.Models;

public class MessageDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserNickname { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public Guid RoomId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
}

public class MessagesResponse
{
    public Guid RoomId { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public int Count { get; set; }
    public DateTime Timestamp { get; set; }
}