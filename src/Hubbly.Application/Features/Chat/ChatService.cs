using Hubbly.Domain.DTOs;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hubbly.Application.Features.Chat;

public class ChatService : IChatService
{
    private readonly IApplicationDbContext _context;
    private readonly IRoomService _roomService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IApplicationDbContext context,
        IRoomService roomService,
        ILogger<ChatService> logger)
    {
        _context = context;
        _roomService = roomService;
        _logger = logger;
    }

    public async Task<MessageDto> SendMessageAsync(Guid userId, Guid roomId, string text)
    {
        // Проверяем, что пользователь в комнате
        var isInRoom = await _roomService.IsUserInRoomAsync(userId, roomId);
        if (!isInRoom)
        {
            // Автоматически добавляем пользователя в комнату
            var joined = await _roomService.JoinRoomAsync(userId, roomId);
            if (!joined)
                throw new UnauthorizedAccessException("Cannot join room or room is full");
        }

        // Проверяем ограничение на длину сообщения
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Message text cannot be empty");

        if (text.Length > 2000)
            throw new ArgumentException("Message cannot exceed 2000 characters");

        // Проверяем rate limiting (5 сообщений в минуту)
        var messagesLastMinute = await _context.Messages
            .CountAsync(m => m.UserId == userId &&
                            m.CreatedAt > DateTime.UtcNow.AddMinutes(-1));

        if (messagesLastMinute >= 5)
            throw new InvalidOperationException("Rate limit exceeded. Please wait before sending more messages.");

        // Создаем сообщение
        var message = new Message
        {
            Text = text.Trim(),
            UserId = userId,
            RoomId = roomId
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Получаем данные пользователя для DTO
        var user = await _context.Users.FindAsync(userId);

        _logger.LogInformation("Message {MessageId} sent by {UserId} in room {RoomId}",
            message.Id, userId, roomId);

        return new MessageDto
        {
            Id = message.Id,
            Text = message.Text,
            UserId = userId,
            UserNickname = user?.Nickname ?? "Unknown",
            UserAvatarUrl = user?.AvatarUrl,
            RoomId = roomId,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<List<MessageDto>> GetRoomHistoryAsync(Guid roomId, int limit = 50)
    {
        // Только по запросу пользователя, не при входе в комнату
        var messages = await _context.Messages
            .Include(m => m.User)
            .Where(m => m.RoomId == roomId && !m.IsDeleted && !m.IsModerated)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return messages
            .OrderBy(m => m.CreatedAt) // Возвращаем в хронологическом порядке
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Text = m.Text,
                UserId = m.UserId,
                UserNickname = m.User?.Nickname ?? "Unknown",
                UserAvatarUrl = m.User?.AvatarUrl,
                RoomId = m.RoomId,
                CreatedAt = m.CreatedAt
            })
            .ToList();
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId, string? reason = null)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.IsDeleted)
            return false;

        message.IsDeleted = true;
        message.IsModerated = !string.IsNullOrEmpty(reason);
        message.DeleteReason = reason;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Message {MessageId} deleted. Reason: {Reason}", messageId, reason ?? "User action");

        return true;
    }

    public async Task<List<MessageDto>> GetRecentMessagesAsync(Guid roomId, int count = 20)
    {
        // Получаем последние N сообщений комнаты
        var messages = await _context.Messages
            .Include(m => m.User)
            .Where(m => m.RoomId == roomId && !m.IsDeleted && !m.IsModerated)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();

        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Text = m.Text,
                UserId = m.UserId,
                UserNickname = m.User?.Nickname ?? "Unknown",
                UserAvatarUrl = m.User?.AvatarUrl,
                RoomId = m.RoomId,
                CreatedAt = m.CreatedAt
            })
            .ToList();
    }

    // Фоновая задача для очистки старых сообщений (можно вынести в BackgroundService)
    public async Task CleanupOldMessagesAsync(TimeSpan olderThan)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;

        var oldMessages = await _context.Messages
            .Where(m => m.CreatedAt < cutoffDate && !m.IsDeleted)
            .Take(1000) // Ограничиваем на одну итерацию
            .ToListAsync();

        foreach (var message in oldMessages)
        {
            message.IsDeleted = true;
            message.DeleteReason = "Auto-cleanup (older than 30 days)";
        }

        if (oldMessages.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} old messages", oldMessages.Count);
        }
    }
}