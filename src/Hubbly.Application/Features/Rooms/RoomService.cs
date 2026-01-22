using Hubbly.Domain.DTOs;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Enums;
using Hubbly.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hubbly.Application.Features.Rooms;

public class RoomService : IRoomService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RoomService> _logger;
    private readonly OnlineUsersService _onlineUsersService;

    public RoomService(
        IApplicationDbContext context,
        ILogger<RoomService> logger,
        OnlineUsersService onlineUsersService)
    {
        _context = context;
        _logger = logger;
        _onlineUsersService = onlineUsersService;
    }

    public async Task<bool> JoinRoomAsync(Guid userId, Guid roomId)
    {
        // 1. Проверяем существование комнаты
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null || room.IsDeleted)
        {
            _logger.LogWarning("Room {RoomId} not found", roomId);
            return false;
        }

        // 2. Проверяем лимит через Redis
        var onlineCount = await _onlineUsersService.GetOnlineCountAsync(roomId);
        if (onlineCount >= room.MaxUsers)
        {
            _logger.LogWarning("Room {RoomId} is full ({Current}/{Max})",
                roomId, onlineCount, room.MaxUsers);
            return false;
        }

        // 3. Проверяем, может пользователь уже онлайн
        var isAlreadyOnline = await _onlineUsersService.IsUserOnlineAsync(roomId, userId);
        if (isAlreadyOnline)
        {
            _logger.LogDebug("User {UserId} already online in room {RoomId}", userId, roomId);
            return true;
        }

        // 4. Обновляем last_room_id у пользователя
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastRoomId = roomId;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("User {UserId} joined room {RoomId} (online: {OnlineCount}/{Max})",
            userId, roomId, onlineCount + 1, room.MaxUsers);

        return true;
    }

    public async Task<bool> LeaveRoomAsync(Guid userId, Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null || room.IsDeleted)
            return false;

        _logger.LogDebug("User {UserId} left room {RoomId}", userId, roomId);
        return true;
    }

    public async Task<bool> IsUserInRoomAsync(Guid userId, Guid roomId)
    {
        // Используем Redis для проверки онлайн статуса
        return await _onlineUsersService.IsUserOnlineAsync(roomId, userId);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid roomId)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);

        if (room == null)
            return null;

        // Получаем актуальный онлайн счетчик из Redis
        var onlineCount = await _onlineUsersService.GetOnlineCountAsync(roomId);

        var dto = MapToDto(room);
        dto.CurrentUsersCount = onlineCount; // Используем актуальное значение из Redis

        return dto;
    }

    public async Task<RoomDto> GetOrCreateAvailableSystemRoomAsync()
    {
        // Ищем системную комнату со свободным местом
        var systemRooms = await _context.Rooms
            .Where(r => r.Type == RoomType.System && !r.IsDeleted)
            .ToListAsync();

        foreach (var room in systemRooms)
        {
            var onlineCount = await _onlineUsersService.GetOnlineCountAsync(room.Id);
            if (onlineCount < room.MaxUsers)
            {
                var dto = MapToDto(room);
                dto.CurrentUsersCount = onlineCount;
                return dto;
            }
        }

        // Создаем новую комнату если все заполнены
        var systemRoomsCount = systemRooms.Count;
        var newRoom = new Room
        {
            Name = systemRoomsCount == 0 ? "Новички" : $"Новички {systemRoomsCount + 1}",
            Description = "Добро пожаловать в Hubbly!",
            Type = RoomType.System,
            MaxUsers = 100,
            CurrentUsersCount = 0
        };

        _context.Rooms.Add(newRoom);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new system room {RoomId}: {RoomName}",
            newRoom.Id, newRoom.Name);

        return MapToDto(newRoom);
    }

    // Вспомогательный метод
    private static RoomDto MapToDto(Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            Type = room.Type,
            MaxUsers = room.MaxUsers,
            CurrentUsersCount = room.CurrentUsersCount,
            CreatorId = room.CreatorId,
            CreatedAt = room.CreatedAt
        };
    }

    public Task<List<RoomDto>> GetAllRoomsAsync()
    {
        throw new NotImplementedException();
    }

    public Task UpdateUsersCountAsync(Guid roomId)
    {
        throw new NotImplementedException();
    }

    public Task SyncAllRoomsCountAsync()
    {
        throw new NotImplementedException();
    }
}