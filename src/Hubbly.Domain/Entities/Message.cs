using Hubbly.Domain.Common;
using Hubbly.Domain.Enums;

namespace Hubbly.Domain.Entities;

public class Message : BaseEntity
{
    public string Text { get; set; } = string.Empty;

    // Отправитель
    public Guid SenderId { get; set; }
    public virtual User Sender { get; set; } = null!;

    // Комната
    public Guid RoomId { get; set; }
    public virtual ChatRoom Room { get; set; } = null!;

    // Тип сообщения (текст, изображение, файл и т.д.)
    public MessageType Type { get; set; } = MessageType.Text;

    // Для ответов на сообщения
    public Guid? ReplyToMessageId { get; set; }
    public virtual Message? ReplyToMessage { get; set; }

    // Метаданные
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? EditedAt { get; set; }
}