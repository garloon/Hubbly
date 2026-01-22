using Hubbly.Domain.DTOs;

namespace Hubbly.Domain.Interfaces;

public interface IRoomService
{
    // Найти или создать системную комнату со свободным местом
    Task<RoomDto> GetOrCreateAvailableSystemRoomAsync();

    // Получить комнату по ID
    Task<RoomDto?> GetRoomByIdAsync(Guid roomId);

    // Список всех комнат
    Task<List<RoomDto>> GetAllRoomsAsync();

    // Присоединить пользователя к комнате
    Task<bool> JoinRoomAsync(Guid userId, Guid roomId);

    // Покинуть комнату
    Task<bool> LeaveRoomAsync(Guid userId, Guid roomId);

    // Обновить счетчик пользователей
    Task UpdateUsersCountAsync(Guid roomId);

    // Проверить, находится ли пользователь в комнате
    Task<bool> IsUserInRoomAsync(Guid userId, Guid roomId);

    Task SyncAllRoomsCountAsync();
}