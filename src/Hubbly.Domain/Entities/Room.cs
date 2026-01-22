using Hubbly.Domain.Common;
using Hubbly.Domain.Enums;

namespace Hubbly.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public RoomType Type { get; set; } = RoomType.System;

    // Лимит пользователей
    public int MaxUsers { get; set; } = 100;

    // Текущее количество пользователей (кэшируем для производительности)
    public int CurrentUsersCount { get; set; }

    // Создатель (null для системных комнат)
    public Guid? CreatorId { get; set; }

    // Инвайт-код для приватных комнат (позже)
    public string? InviteCode { get; set; }

    // Навигационные свойства
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    // Метод проверки, есть ли свободные места
    public bool HasAvailableSpace() => CurrentUsersCount < MaxUsers;
}