using Hubbly.Domain.Entities;
using Hubbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Hubbly.Domain.Interfaces;
using Hubbly.Domain.Dtos.Rooms;

namespace Hubbly.Application.Features.Rooms;

public class RoomService : IRoomService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RoomService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RoomDto> CreateRoomAsync(Guid userId, CreateRoomDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        var room = new ChatRoom
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            CreatorId = userId,
            MaxMembers = request.MaxMembers
        };

        // Для приватных комнат генерируем инвайт-код
        if (request.Type == RoomType.Private)
        {
            room.InviteCode = GenerateInviteCode();
            room.InviteCodeExpires = DateTime.UtcNow.AddDays(7);
        }

        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();

        // Автоматически добавляем создателя как участника и админа
        var roomMember = new RoomMember
        {
            UserId = userId,
            RoomId = room.Id,
            IsAdmin = true
        };

        _context.RoomMembers.Add(roomMember);
        await _context.SaveChangesAsync();

        return await MapToRoomDtoAsync(room, userId);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid roomId, Guid? userId = null)
    {
        var room = await _context.ChatRooms
            .Include(r => r.Creator)
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);

        if (room == null) return null;

        return await MapToRoomDtoAsync(room, userId);
    }

    public async Task<RoomDetailsDto?> GetRoomDetailsAsync(Guid roomId, Guid userId)
    {
        var room = await _context.ChatRooms
            .Include(r => r.Creator)
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);

        if (room == null) return null;

        // Проверяем доступ (для приватных комнат)
        if (room.Type == RoomType.Private)
        {
            var isMember = await _context.RoomMembers
                .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId && !rm.IsBanned);

            if (!isMember && room.CreatorId != userId)
                throw new UnauthorizedAccessException("Access denied to private room");
        }

        var dto = new RoomDetailsDto
        {
            Id = room.Id,
            Title = room.Title,
            Description = room.Description,
            Type = room.Type,
            CreatorId = room.CreatorId,
            CreatorName = room.Creator.DisplayName,
            CreatorAvatarUrl = room.Creator.AvatarUrl,
            MaxMembers = room.MaxMembers,
            CreatedAt = room.CreatedAt,
            HasPassword = false, // Пока не реализовано
            InviteCode = room.InviteCode,
            InviteCodeExpires = room.InviteCodeExpires,
            LastActivityAt = await GetLastActivityAsync(roomId)
        };

        // Заполняем участников
        dto.Members = room.Members
            .Where(m => !m.IsBanned)
            .Select(m => new RoomMemberDto
            {
                UserId = m.UserId,
                DisplayName = m.User.DisplayName,
                AvatarUrl = m.User.AvatarUrl,
                IsAdmin = m.IsAdmin,
                IsCreator = m.UserId == room.CreatorId,
                JoinedAt = m.JoinedAt
            })
            .OrderByDescending(m => m.IsCreator)
            .ThenByDescending(m => m.IsAdmin)
            .ThenBy(m => m.JoinedAt)
            .ToList();

        dto.MemberCount = dto.Members.Count;
        dto.OnlineCount = dto.Members.Count; // TODO: Реализовать онлайн статус
        dto.IsMember = dto.Members.Any(m => m.UserId == userId);
        dto.IsAdmin = dto.Members.FirstOrDefault(m => m.UserId == userId)?.IsAdmin ?? false;

        return dto;
    }

    public async Task<IEnumerable<RoomDto>> GetPublicRoomsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var rooms = await _context.ChatRooms
            .Include(r => r.Creator)
            .Where(r => r.Type == RoomType.Public && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<RoomDto>();
        foreach (var room in rooms)
        {
            result.Add(await MapToRoomDtoAsync(room, userId));
        }

        return result;
    }

    public async Task<IEnumerable<RoomDto>> GetUserRoomsAsync(Guid userId)
    {
        var roomIds = await _context.RoomMembers
            .Where(rm => rm.UserId == userId && !rm.IsBanned)
            .Select(rm => rm.RoomId)
            .ToListAsync();

        var rooms = await _context.ChatRooms
            .Include(r => r.Creator)
            .Where(r => roomIds.Contains(r.Id) && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<RoomDto>();
        foreach (var room in rooms)
        {
            result.Add(await MapToRoomDtoAsync(room, userId));
        }

        return result;
    }

    public async Task<bool> JoinRoomAsync(Guid userId, Guid roomId, string? inviteCode = null)
    {
        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room == null || room.IsDeleted)
            return false;

        // Проверяем ограничение по количеству участников
        if (room.MaxMembers > 0)
        {
            var memberCount = await _context.RoomMembers
                .CountAsync(rm => rm.RoomId == roomId && !rm.IsBanned);

            if (memberCount >= room.MaxMembers)
                return false;
        }

        // Проверяем тип комнаты
        if (room.Type == RoomType.Private)
        {
            if (string.IsNullOrEmpty(inviteCode) || room.InviteCode != inviteCode)
                return false;

            // Проверяем срок действия инвайт-кода
            if (room.InviteCodeExpires.HasValue && room.InviteCodeExpires < DateTime.UtcNow)
                return false;
        }

        // Проверяем, не забанен ли пользователь
        var existingMember = await _context.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (existingMember != null)
        {
            if (existingMember.IsBanned)
                return false;

            // Уже участник
            return true;
        }

        // Добавляем как участника
        var roomMember = new RoomMember
        {
            UserId = userId,
            RoomId = roomId,
            IsAdmin = false
        };

        _context.RoomMembers.Add(roomMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LeaveRoomAsync(Guid userId, Guid roomId)
    {
        var roomMember = await _context.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (roomMember == null)
            return false;

        // Создатель не может покинуть комнату (должен удалить её)
        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room?.CreatorId == userId)
            return false;

        _context.RoomMembers.Remove(roomMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<RoomDto?> GetNoviceRoomAsync(Guid userId)
    {
        var room = await _context.ChatRooms
            .Include(r => r.Creator)
            .FirstOrDefaultAsync(r => r.Type == RoomType.SystemNovice && !r.IsDeleted);

        if (room == null)
            return null;

        return await MapToRoomDtoAsync(room, userId);
    }

    // Вспомогательные методы
    private async Task<RoomDto> MapToRoomDtoAsync(ChatRoom room, Guid? userId = null)
    {
        var memberCount = await _context.Set<RoomMember>()
            .CountAsync(rm => rm.RoomId == room.Id && !rm.IsBanned);

        var isMember = userId.HasValue && await _context.RoomMembers
            .AnyAsync(rm => rm.RoomId == room.Id && rm.UserId == userId && !rm.IsBanned);

        var isAdmin = userId.HasValue && await _context.RoomMembers
            .AnyAsync(rm => rm.RoomId == room.Id && rm.UserId == userId && rm.IsAdmin && !rm.IsBanned);

        return new RoomDto
        {
            Id = room.Id,
            Title = room.Title,
            Description = room.Description,
            Type = room.Type,
            CreatorId = room.CreatorId,
            CreatorName = room.Creator?.DisplayName ?? "Unknown",
            CreatorAvatarUrl = room.Creator?.AvatarUrl,
            MemberCount = memberCount,
            OnlineCount = memberCount, // TODO: Реализовать онлайн статус
            MaxMembers = room.MaxMembers,
            IsMember = isMember,
            IsAdmin = isAdmin,
            CreatedAt = room.CreatedAt,
            LastActivityAt = await GetLastActivityAsync(room.Id)
        };
    }

    private async Task<DateTime?> GetLastActivityAsync(Guid roomId)
    {
        var lastMessage = await _context.Messages
            .Where(m => m.RoomId == roomId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        return lastMessage?.CreatedAt;
    }

    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    // Остальные методы интерфейса пока заглушки
    public Task<RoomDto> UpdateRoomAsync(Guid roomId, Guid userId, UpdateRoomDto request)
        => throw new NotImplementedException();

    public Task<bool> DeleteRoomAsync(Guid roomId, Guid userId)
        => throw new NotImplementedException();

    public Task<bool> KickUserAsync(Guid roomId, Guid adminUserId, Guid targetUserId)
        => throw new NotImplementedException();

    public Task<bool> ToggleAdminAsync(Guid roomId, Guid adminUserId, Guid targetUserId, bool makeAdmin)
        => throw new NotImplementedException();

    public Task<string> GenerateInviteCodeAsync(Guid roomId, Guid userId, TimeSpan? expiry = null)
        => throw new NotImplementedException();

    public Task<bool> ValidateInviteCodeAsync(Guid roomId, string inviteCode)
        => throw new NotImplementedException();

    public Task<IEnumerable<RoomDto>> SearchRoomsAsync(string query, Guid userId, int limit = 20)
        => throw new NotImplementedException();
}