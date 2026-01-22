using Hubbly.Domain.Common;

namespace Hubbly.Domain.Entities;

public class User : BaseEntity
{
    // Идентификатор устройства для гостей
    public string? DeviceId { get; set; }

    // Email (опционально, для зарегистрированных)
    public string? Email { get; set; }

    // Никнейм (обязательно)
    public string Nickname { get; set; } = string.Empty;

    // Аватар (опционально)
    public string? AvatarUrl { get; set; }

    // Последняя комната, где был пользователь
    public Guid? LastRoomId { get; set; }

    // Навигационные свойства
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    // Время последней активности (для очистки неактивных)
    public DateTime? LastActivityAt { get; set; }
}