using Hubbly.Domain.Common;
using Hubbly.Domain.Enums;

namespace Hubbly.Domain.Entities;

public class ChatRoom : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public RoomType Type { get; set; } = RoomType.Public;

    // Ссылка на создателя
    public Guid CreatorId { get; set; }
    public virtual User Creator { get; set; } = null!;

    // Участники комнаты (через промежуточную таблицу RoomMember)
    public virtual ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();

    // Сообщения в комнате
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    // Администраторы (отдельная связь, можно хранить в RoomMember с флагом IsAdmin)

    // Для приватных комнат - инвайт-код
    public string? InviteCode { get; set; }
    public DateTime? InviteCodeExpires { get; set; }

    // Максимальное количество участников (0 = без ограничений)
    public int MaxMembers { get; set; } = 0;
}