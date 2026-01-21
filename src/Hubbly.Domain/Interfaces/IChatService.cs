using Hubbly.Domain.Dtos.Messages;

namespace Hubbly.Domain.Interfaces;

public interface IChatService
{
    // Основные операции с сообщениями
    Task<MessageDto> SendMessageAsync(Guid userId, Guid roomId, string text);
    Task<MessageDto> SendMessageAsync(Guid userId, Guid roomId, SendMessageDto dto);
    Task<EditMessageResult> EditMessageAsync(Guid userId, Guid messageId, string newText);
    Task<DeleteMessageResult> DeleteMessageAsync(Guid userId, Guid messageId);

    // Получение сообщений
    Task<IEnumerable<MessageDto>> GetRoomMessagesAsync(Guid roomId, Guid userId, int page = 1, int pageSize = 50);
    Task<MessageDto?> GetMessageByIdAsync(Guid messageId, Guid userId);

    // Управление комнатами
    Task<bool> JoinRoomAsync(Guid userId, Guid roomId);
    Task<bool> LeaveRoomAsync(Guid userId, Guid roomId);

    // Статистика
    Task<int> GetUnreadCountAsync(Guid userId, Guid roomId);
    Task MarkAsReadAsync(Guid userId, Guid roomId, Guid messageId);
}