using Hubbly.Domain.DTOs;

namespace Hubbly.Domain.Interfaces;

public interface IChatService
{
    // Отправить сообщение (только live, без сохранения в историю при входе)
    Task<MessageDto> SendMessageAsync(Guid userId, Guid roomId, string text);

    // Получить историю сообщений (только по запросу пользователя)
    Task<List<MessageDto>> GetRoomHistoryAsync(Guid roomId, int limit = 50);

    // Удалить сообщение (модерация)
    Task<bool> DeleteMessageAsync(Guid messageId, string? reason = null);

    // Получить последние N сообщений комнаты
    Task<List<MessageDto>> GetRecentMessagesAsync(Guid roomId, int count = 20);
}