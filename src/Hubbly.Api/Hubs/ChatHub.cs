using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Hubbly.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, connectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected", userId);

        // Удаляем пользователя из всех комнат при отключении
        await RemoveUserFromAllRooms(userId);

        await base.OnDisconnectedAsync(exception);
    }

    // Присоединение к комнате
    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetUserId();

        try
        {
            var success = await _chatService.JoinRoomAsync(userId, roomId);

            if (success)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

                // Уведомляем всех в комнате о новом участнике
                await Clients.Group(roomId.ToString()).SendAsync("UserJoined", new
                {
                    UserId = userId,
                    RoomId = roomId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room {RoomId} for user {UserId}", roomId, userId);
            throw new HubException($"Failed to join room: {ex.Message}");
        }
    }

    // Покидание комнаты
    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserId();

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

            // Уведомляем всех в комнате
            await Clients.Group(roomId.ToString()).SendAsync("UserLeft", new
            {
                UserId = userId,
                RoomId = roomId,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving room {RoomId} for user {UserId}", roomId, userId);
            throw new HubException($"Failed to leave room: {ex.Message}");
        }
    }

    // Отправка сообщения
    public async Task SendMessage(Guid roomId, string text)
    {
        var userId = GetUserId();

        try
        {
            var message = await _chatService.SendMessageAsync(userId, roomId, text);

            // Отправляем сообщение всем в комнате
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);

            _logger.LogInformation("Message sent to room {RoomId} by user {UserId}", roomId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to room {RoomId} for user {UserId}", roomId, userId);
            throw new HubException($"Failed to send message: {ex.Message}");
        }
    }

    // Редактирование сообщения
    public async Task EditMessage(Guid messageId, string newText)
    {
        var userId = GetUserId();

        try
        {
            var result = await _chatService.EditMessageAsync(userId, messageId, newText);

            if (result.Success)
            {
                // Уведомляем об изменении
                await Clients.Group(result.RoomId.ToString()).SendAsync("MessageEdited", new
                {
                    MessageId = messageId,
                    NewText = newText,
                    EditedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId} by user {UserId}", messageId, userId);
            throw new HubException($"Failed to edit message: {ex.Message}");
        }
    }

    // Удаление сообщения
    public async Task DeleteMessage(Guid messageId)
    {
        var userId = GetUserId();

        try
        {
            var result = await _chatService.DeleteMessageAsync(userId, messageId);

            if (result.Success)
            {
                await Clients.Group(result.RoomId.ToString()).SendAsync("MessageDeleted", new
                {
                    MessageId = messageId,
                    DeletedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId} by user {UserId}", messageId, userId);
            throw new HubException($"Failed to delete message: {ex.Message}");
        }
    }

    // Типинг индикатор (пользователь печатает)
    public async Task Typing(Guid roomId, bool isTyping)
    {
        var userId = GetUserId();

        await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserTyping", new
        {
            UserId = userId,
            RoomId = roomId,
            IsTyping = isTyping,
            Timestamp = DateTime.UtcNow
        });
    }

    // Вспомогательные методы
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("User not authenticated");
        }
        return userId;
    }

    private async Task RemoveUserFromAllRooms(Guid userId)
    {
        // Здесь можно реализовать логику удаления пользователя из комнат
        // Например, через сервис чата
    }
}