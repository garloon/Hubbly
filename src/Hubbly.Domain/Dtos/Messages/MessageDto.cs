using Hubbly.Domain.Enums;

namespace Hubbly.Domain.Dtos.Messages;

public class MessageDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public Guid RoomId { get; set; }
    public MessageType Type { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public MessageDto? ReplyToMessage { get; set; }
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}

public class SendMessageDto
{
    public string Text { get; set; } = string.Empty;
    public Guid? ReplyToMessageId { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
}

public class EditMessageDto
{
    public string Text { get; set; } = string.Empty;
}

public class EditMessageResult
{
    public bool Success { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? RoomId { get; set; }
    public string? Error { get; set; }
}

public class DeleteMessageResult
{
    public bool Success { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? RoomId { get; set; }
    public string? Error { get; set; }
}