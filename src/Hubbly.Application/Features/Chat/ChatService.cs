using Hubbly.Domain.Dtos.Messages;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Enums;
using Hubbly.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hubbly.Application.Features.Chat;

public class ChatService : IChatService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ChatService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<MessageDto> SendMessageAsync(Guid userId, Guid roomId, string text)
    {
        return await SendMessageAsync(userId, roomId, new SendMessageDto { Text = text });
    }

    public async Task<MessageDto> SendMessageAsync(Guid userId, Guid roomId, SendMessageDto dto)
    {
        // Проверяем доступ к комнате
        var hasAccess = await HasAccessToRoomAsync(userId, roomId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("No access to room");

        var user = await _context.Users.FindAsync(userId);
        var room = await _context.ChatRooms.FindAsync(roomId);

        if (user == null || room == null)
            throw new ArgumentException("User or room not found");

        var message = new Message
        {
            Text = dto.Text,
            SenderId = userId,
            RoomId = roomId,
            Type = dto.Type,
            ReplyToMessageId = dto.ReplyToMessageId
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Message {MessageId} sent by {UserId} in room {RoomId}",
            message.Id, userId, roomId);

        return await MapToMessageDtoAsync(message);
    }

    public async Task<EditMessageResult> EditMessageAsync(Guid userId, Guid messageId, string newText)
    {
        try
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

            if (message == null)
                return new EditMessageResult { Success = false, Error = "Message not found" };

            // Проверяем права
            if (message.SenderId != userId)
                return new EditMessageResult { Success = false, Error = "Can only edit own messages" };

            // Проверяем временные ограничения
            var timeSinceCreation = DateTime.UtcNow - message.CreatedAt;
            if (timeSinceCreation > TimeSpan.FromMinutes(15))
                return new EditMessageResult { Success = false, Error = "Message can only be edited within 15 minutes of creation" };

            message.Text = newText;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} edited by {UserId}", messageId, userId);

            return new EditMessageResult
            {
                Success = true,
                MessageId = messageId,
                RoomId = message.RoomId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId}", messageId);
            return new EditMessageResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<DeleteMessageResult> DeleteMessageAsync(Guid userId, Guid messageId)
    {
        try
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

            if (message == null)
                return new DeleteMessageResult { Success = false, Error = "Message not found" };

            // Проверяем права (отправитель или админ комнаты)
            var isAdmin = await IsRoomAdminAsync(userId, message.RoomId);
            if (message.SenderId != userId && !isAdmin)
                return new DeleteMessageResult { Success = false, Error = "No permission to delete this message" };

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} deleted by {UserId}", messageId, userId);

            return new DeleteMessageResult
            {
                Success = true,
                MessageId = messageId,
                RoomId = message.RoomId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            return new DeleteMessageResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<IEnumerable<MessageDto>> GetRoomMessagesAsync(
        Guid roomId, Guid userId, int page = 1, int pageSize = 50)
    {
        // Проверяем доступ
        var hasAccess = await HasAccessToRoomAsync(userId, roomId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("No access to room");

        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
            .Where(m => m.RoomId == roomId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<MessageDto>();
        foreach (var message in messages.OrderBy(m => m.CreatedAt)) // Возвращаем в правильном порядке
        {
            result.Add(await MapToMessageDtoAsync(message));
        }

        return result;
    }

    public async Task<MessageDto?> GetMessageByIdAsync(Guid messageId, Guid userId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message == null)
            return null;

        // Проверяем доступ к комнате
        var hasAccess = await HasAccessToRoomAsync(userId, message.RoomId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("No access to room");

        return await MapToMessageDtoAsync(message);
    }

    public async Task<bool> JoinRoomAsync(Guid userId, Guid roomId)
    {
        // Используем RoomService или дублируем логику
        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room == null || room.IsDeleted)
            return false;

        // Проверяем приватность
        if (room.Type == RoomType.Private)
        {
            var isMember = await _context.RoomMembers
                .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId && !rm.IsBanned);

            if (!isMember && room.CreatorId != userId)
                return false;
        }

        // Проверяем существующее членство
        var existing = await _context.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (existing != null)
        {
            if (existing.IsBanned)
                return false;
            return true;
        }

        // Добавляем
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

        // Создатель не может покинуть комнату
        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room?.CreatorId == userId)
            return false;

        _context.RoomMembers.Remove(roomMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid roomId)
    {
        // TODO: Реализовать логику непрочитанных сообщений
        // Нужно хранить время последнего прочтения для каждого пользователя в каждой комнате
        return 0;
    }

    public async Task MarkAsReadAsync(Guid userId, Guid roomId, Guid messageId)
    {
        // TODO: Реализовать отметку прочитанных сообщений
    }

    // Вспомогательные методы
    private async Task<bool> HasAccessToRoomAsync(Guid userId, Guid roomId)
    {
        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room == null || room.IsDeleted)
            return false;

        // Публичные комнаты доступны всем
        if (room.Type == RoomType.Public)
            return true;

        // Для приватных проверяем членство
        return await _context.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId && !rm.IsBanned);
    }

    private async Task<bool> IsRoomAdminAsync(Guid userId, Guid roomId)
    {
        return await _context.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId && rm.IsAdmin && !rm.IsBanned);
    }

    private async Task<MessageDto> MapToMessageDtoAsync(Message message)
    {
        var dto = new MessageDto
        {
            Id = message.Id,
            Text = message.Text,
            SenderId = message.SenderId,
            SenderName = message.Sender?.DisplayName ?? "Unknown",
            SenderAvatarUrl = message.Sender?.AvatarUrl,
            RoomId = message.RoomId,
            Type = message.Type,
            ReplyToMessageId = message.ReplyToMessageId,
            IsEdited = message.IsEdited,
            CreatedAt = message.CreatedAt,
            EditedAt = message.EditedAt
        };

        // Добавляем информацию о сообщении-ответе если есть
        if (message.ReplyToMessage != null)
        {
            dto.ReplyToMessage = new MessageDto
            {
                Id = message.ReplyToMessage.Id,
                Text = message.ReplyToMessage.Text,
                SenderId = message.ReplyToMessage.SenderId,
                SenderName = message.ReplyToMessage.Sender?.DisplayName ?? "Unknown",
                CreatedAt = message.ReplyToMessage.CreatedAt
            };
        }

        return dto;
    }
}