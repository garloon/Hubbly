using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Hubbly.Application.Features.Rooms;

public class OnlineUsersService
{
    private readonly IDatabase _redis;
    private readonly ILogger<OnlineUsersService> _logger;
    private readonly TimeSpan _onlineTimeout = TimeSpan.FromMinutes(5);

    public OnlineUsersService(
        IConnectionMultiplexer redis,
        ILogger<OnlineUsersService> logger)
    {
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    // Ключи для Redis
    private static string RoomOnlineKey(Guid roomId) => $"room_online:{roomId}";
    private static string UserConnectionKey(Guid userId) => $"user_connection:{userId}";
    private static string ConnectionUserKey(string connectionId) => $"connection_user:{connectionId}";
    private static string UserRoomsKey(Guid userId) => $"user_rooms:{userId}";

    // 1. Управление подключениями
    public async Task SaveUserConnectionAsync(Guid userId, string connectionId)
    {
        try
        {
            // Связь UserId -> ConnectionId
            await _redis.StringSetAsync(
                UserConnectionKey(userId),
                connectionId,
                _onlineTimeout);

            // Связь ConnectionId -> UserId
            await _redis.StringSetAsync(
                ConnectionUserKey(connectionId),
                userId.ToString(),
                _onlineTimeout);

            _logger.LogDebug("Saved connection {ConnectionId} for user {UserId}",
                connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user connection");
        }
    }

    public async Task<string?> GetUserConnectionAsync(Guid userId)
    {
        try
        {
            return await _redis.StringGetAsync(UserConnectionKey(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user connection");
            return null;
        }
    }

    public async Task<Guid?> GetUserByConnectionAsync(string connectionId)
    {
        try
        {
            var userIdStr = await _redis.StringGetAsync(ConnectionUserKey(connectionId));
            return Guid.TryParse(userIdStr, out var userId) ? userId : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by connection");
            return null;
        }
    }

    public async Task RemoveUserConnectionAsync(string connectionId)
    {
        try
        {
            var userId = await GetUserByConnectionAsync(connectionId);
            if (userId.HasValue)
            {
                await _redis.KeyDeleteAsync(UserConnectionKey(userId.Value));
            }
            await _redis.KeyDeleteAsync(ConnectionUserKey(connectionId));

            _logger.LogDebug("Removed connection {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user connection");
        }
    }

    // 2. Управление онлайн статусом в комнатах
    public async Task MarkUserOnlineAsync(Guid roomId, Guid userId)
    {
        try
        {
            var key = RoomOnlineKey(roomId);
            await _redis.HashSetAsync(key, userId.ToString(), DateTime.UtcNow.Ticks);
            await _redis.KeyExpireAsync(key, _onlineTimeout);

            // Сохраняем комнату пользователя
            await AddUserRoomAsync(userId, roomId);

            _logger.LogDebug("User {UserId} marked online in room {RoomId}", userId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user online");
        }
    }

    public async Task MarkUserOfflineAsync(Guid roomId, Guid userId)
    {
        try
        {
            var key = RoomOnlineKey(roomId);
            await _redis.HashDeleteAsync(key, userId.ToString());

            // Удаляем комнату из списка пользователя
            await RemoveUserRoomAsync(userId, roomId);

            _logger.LogDebug("User {UserId} marked offline in room {RoomId}", userId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user offline");
        }
    }

    // 3. Управление списком комнат пользователя
    private async Task AddUserRoomAsync(Guid userId, Guid roomId)
    {
        try
        {
            var key = UserRoomsKey(userId);
            await _redis.SetAddAsync(key, roomId.ToString());
            await _redis.KeyExpireAsync(key, _onlineTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user room");
        }
    }

    private async Task RemoveUserRoomAsync(Guid userId, Guid roomId)
    {
        try
        {
            var key = UserRoomsKey(userId);
            await _redis.SetRemoveAsync(key, roomId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user room");
        }
    }

    public async Task<List<Guid>> GetUserRoomsAsync(Guid userId)
    {
        try
        {
            var key = UserRoomsKey(userId);
            var roomIds = await _redis.SetMembersAsync(key);

            var result = new List<Guid>();
            foreach (var roomIdStr in roomIds)
            {
                if (Guid.TryParse(roomIdStr, out var roomId))
                {
                    result.Add(roomId);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user rooms");
            return new List<Guid>();
        }
    }

    // 4. Получение статистики
    public async Task<int> GetOnlineCountAsync(Guid roomId)
    {
        try
        {
            var key = RoomOnlineKey(roomId);
            var entries = await _redis.HashGetAllAsync(key);

            if (!entries.Any())
                return 0;

            // Фильтруем устаревшие записи
            var cutoff = DateTime.UtcNow.AddMinutes(-5).Ticks;
            var activeUsers = 0;

            foreach (var entry in entries)
            {
                if (entry.Value.HasValue && (long)entry.Value > cutoff)
                {
                    activeUsers++;
                }
            }

            return activeUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online count");
            return 0;
        }
    }

    public async Task<List<Guid>> GetOnlineUsersAsync(Guid roomId)
    {
        try
        {
            var key = RoomOnlineKey(roomId);
            var entries = await _redis.HashGetAllAsync(key);

            var cutoff = DateTime.UtcNow.AddMinutes(-5).Ticks;
            var activeUsers = new List<Guid>();

            foreach (var entry in entries)
            {
                if (entry.Value.HasValue &&
                    (long)entry.Value > cutoff &&
                    Guid.TryParse(entry.Name, out var userId))
                {
                    activeUsers.Add(userId);
                }
            }

            return activeUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online users");
            return new List<Guid>();
        }
    }

    public async Task<bool> IsUserOnlineAsync(Guid roomId, Guid userId)
    {
        try
        {
            var key = RoomOnlineKey(roomId);
            var lastSeenTicks = await _redis.HashGetAsync(key, userId.ToString());

            if (lastSeenTicks.HasValue)
            {
                var lastSeen = new DateTime((long)lastSeenTicks);
                return DateTime.UtcNow - lastSeen < _onlineTimeout;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user online status");
            return false;
        }
    }

    // 5. Глобальный онлайн статус
    public async Task MarkUserOnlineGloballyAsync(Guid userId)
    {
        try
        {
            var key = $"user_global_online:{userId}";
            await _redis.StringSetAsync(key, DateTime.UtcNow.Ticks, _onlineTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user globally online");
        }
    }

    public async Task MarkUserOfflineGloballyAsync(Guid userId)
    {
        try
        {
            var key = $"user_global_online:{userId}";
            await _redis.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user globally offline");
        }
    }

    public async Task<bool> IsUserOnlineGloballyAsync(Guid userId)
    {
        try
        {
            var key = $"user_global_online:{userId}";
            var lastSeenTicks = await _redis.StringGetAsync(key);

            if (lastSeenTicks.HasValue)
            {
                var lastSeen = new DateTime((long)lastSeenTicks);
                return DateTime.UtcNow - lastSeen < _onlineTimeout;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking global online status");
            return false;
        }
    }
}