using Hubbly.Domain.DTOs;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Hubbly.Application.Features.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IApplicationDbContext context,
        IDistributedCache cache,
        ILogger<UserService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UserDto> GetOrCreateGuestUserAsync(string deviceId, string nickname)
    {
        // Проверяем кэш на наличие пользователя по deviceId
        var cacheKey = $"user_device_{deviceId}";
        var cachedUserId = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedUserId) && Guid.TryParse(cachedUserId, out var userId))
        {
            var existingUser = await _context.Users.FindAsync(userId);
            if (existingUser != null && !existingUser.IsDeleted)
            {
                _logger.LogInformation("Found existing guest user {UserId} for device {DeviceId}", userId, deviceId);
                return MapToDto(existingUser);
            }
        }

        // Создаем нового гостя
        var user = new User
        {
            DeviceId = deviceId,
            Nickname = ValidateAndFormatNickname(nickname),
            LastActivityAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Сохраняем в кэш на 30 дней
        await _cache.SetStringAsync(cacheKey, user.Id.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });

        _logger.LogInformation("Created new guest user {UserId} with nickname {Nickname}", user.Id, user.Nickname);

        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> UpdateNicknameAsync(Guid userId, string newNickname)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsDeleted)
            throw new ArgumentException("User not found");

        user.Nickname = ValidateAndFormatNickname(newNickname);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated nickname to {Nickname}", userId, user.Nickname);

        return MapToDto(user);
    }

    public async Task<UserDto> RegisterUserAsync(Guid userId, string email)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsDeleted)
            throw new ArgumentException("User not found");

        // Проверяем, что email не занят другим пользователем
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == email && u.Id != userId && !u.IsDeleted);

        if (emailExists)
            throw new InvalidOperationException("Email already registered");

        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} registered with email {Email}", userId, email);

        return MapToDto(user);
    }

    public async Task UpdateLastActivityAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null && !user.IsDeleted)
        {
            user.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CleanupInactiveGuestsAsync(TimeSpan inactivityThreshold)
    {
        var cutoffDate = DateTime.UtcNow - inactivityThreshold;

        var inactiveGuests = await _context.Users
            .Where(u => u.DeviceId != null &&
                       u.Email == null && // Только гости
                       u.LastActivityAt < cutoffDate &&
                       !u.IsDeleted)
            .ToListAsync();

        foreach (var guest in inactiveGuests)
        {
            guest.IsDeleted = true;
            guest.UpdatedAt = DateTime.UtcNow;

            // Удаляем из кэша
            var cacheKey = $"user_device_{guest.DeviceId}";
            await _cache.RemoveAsync(cacheKey);
        }

        if (inactiveGuests.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} inactive guest users", inactiveGuests.Count);
        }
    }

    // Вспомогательные методы
    private static string ValidateAndFormatNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            throw new ArgumentException("Nickname cannot be empty");

        var trimmed = nickname.Trim();

        if (trimmed.Length < 3)
            throw new ArgumentException("Nickname must be at least 3 characters");

        if (trimmed.Length > 50)
            throw new ArgumentException("Nickname cannot exceed 50 characters");

        // Заменяем множественные пробелы на один
        return string.Join(" ", trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Nickname = user.Nickname,
            AvatarUrl = user.AvatarUrl,
            Email = user.Email,
            LastRoomId = user.LastRoomId
        };
    }
}