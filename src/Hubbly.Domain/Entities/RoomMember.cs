using Hubbly.Domain.Common;

namespace Hubbly.Domain.Entities;

public class RoomMember : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoomId { get; set; }

    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    public virtual User User { get; set; } = null!;
    public virtual ChatRoom Room { get; set; } = null!;
}