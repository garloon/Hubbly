using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Models.Shared;

public class MessageDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public Guid RoomId { get; set; }

    // ИЗМЕНИЛИ: был string, стал int
    public int Type { get; set; }

    public Guid? ReplyToMessageId { get; set; }
    public MessageDto? ReplyToMessage { get; set; }
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}