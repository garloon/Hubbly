using Hubbly.Domain.Common;

namespace Hubbly.Domain.Entities;

public class Message : BaseEntity
{
    public string Text { get; set; } = string.Empty;

    // Отправитель
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    // Комната
    public Guid RoomId { get; set; }
    public virtual Room Room { get; set; } = null!;

    // Сообщение удалено (soft delete для архивации)
    public bool IsDeleted { get; set; }

    // Удалено модератором
    public bool IsModerated { get; set; }

    // Причина удаления (для модерации)
    public string? DeleteReason { get; set; }
}