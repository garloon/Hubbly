using Hubbly.Domain.Dtos.Rooms;

namespace Hubbly.Domain.Interfaces;

public interface IRoomService
{
    // Основные операции
    Task<RoomDto> CreateRoomAsync(Guid userId, CreateRoomDto request);
    Task<RoomDto?> GetRoomByIdAsync(Guid roomId, Guid? userId = null);
    Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId, Guid userId);
    Task<IEnumerable<RoomDto>> GetPublicRoomsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<RoomDto>> GetUserRoomsAsync(Guid userId);
    Task<RoomDto> UpdateRoomAsync(Guid roomId, Guid userId, UpdateRoomDto request);
    Task<bool> DeleteRoomAsync(Guid roomId, Guid userId);

    // Управление участниками
    Task<bool> JoinRoomAsync(Guid userId, Guid roomId, string? inviteCode = null);
    Task<bool> LeaveRoomAsync(Guid userId, Guid roomId);
    Task<bool> KickUserAsync(Guid roomId, Guid adminUserId, Guid targetUserId);
    Task<bool> ToggleAdminAsync(Guid roomId, Guid adminUserId, Guid targetUserId, bool makeAdmin);

    // Приглашения
    Task<string> GenerateInviteCodeAsync(Guid roomId, Guid userId, TimeSpan? expiry = null);
    Task<bool> ValidateInviteCodeAsync(Guid roomId, string inviteCode);

    // Поиск
    Task<IEnumerable<RoomDto>> SearchRoomsAsync(string query, Guid userId, int limit = 20);
}