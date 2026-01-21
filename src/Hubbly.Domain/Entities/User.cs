using Hubbly.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Hubbly.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    // Для 3D аватаров (позже)
    public string? Avatar3DData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Навигационные свойства
    public virtual ICollection<ChatRoom> CreatedRooms { get; set; } = new List<ChatRoom>();
    public virtual ICollection<RoomMember> RoomMemberships { get; set; } = new List<RoomMember>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}