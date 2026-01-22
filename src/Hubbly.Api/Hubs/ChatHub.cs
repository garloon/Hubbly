using Hubbly.Application.Features.Rooms;
using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Hubbly.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly IRoomService _roomService;
    private readonly OnlineUsersService _onlineUsersService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IUserService userService,
        IRoomService roomService,
        OnlineUsersService onlineUsersService,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _userService = userService;
        _roomService = roomService;
        _onlineUsersService = onlineUsersService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var deviceId = httpContext?.Request.Headers["X-Device-Id"].ToString();
        var userIdStr = httpContext?.Request.Headers["X-User-Id"].ToString();

        if (Guid.TryParse(userIdStr, out var userId))
        {
            // Сохраняем связь ConnectionId -> UserId
            await _onlineUsersService.SaveUserConnectionAsync(userId, Context.ConnectionId);

            // Отмечаем глобальный онлайн статус
            await _onlineUsersService.MarkUserOnlineGloballyAsync(userId);

            _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            // Получаем userId по connectionId
            var userId = await _onlineUsersService.GetUserByConnectionAsync(Context.ConnectionId);

            if (userId.HasValue)
            {
                // Отмечаем оффлайн во всех комнатах где был пользователь
                var userRooms = await _onlineUsersService.GetUserRoomsAsync(userId.Value);
                foreach (var roomId in userRooms)
                {
                    await _onlineUsersService.MarkUserOfflineAsync(roomId, userId.Value);

                    // Получаем актуальный онлайн счетчик
                    var onlineCount = await _onlineUsersService.GetOnlineCountAsync(roomId);

                    // Уведомляем других пользователей
                    await Clients.Group(roomId.ToString()).SendAsync("UserLeft", new
                    {
                        UserId = userId.Value,
                        RoomId = roomId,
                        OnlineCount = onlineCount,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Отмечаем глобальный оффлайн
                await _onlineUsersService.MarkUserOfflineGloballyAsync(userId.Value);

                // Удаляем связь подключения
                await _onlineUsersService.RemoveUserConnectionAsync(Context.ConnectionId);

                _logger.LogInformation("User {UserId} disconnected from all rooms", userId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection");
        }

        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(Guid roomId, Guid userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                throw new HubException("User not found");

            // Используем RoomService для проверки лимитов
            var success = await _roomService.JoinRoomAsync(userId, roomId);
            if (!success)
                throw new HubException("Failed to join room (room may be full)");

            // Добавляем в группу SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

            // Отмечаем онлайн в Redis
            await _onlineUsersService.MarkUserOnlineAsync(roomId, userId);

            // Получаем актуальный онлайн счетчик
            var onlineCount = await _onlineUsersService.GetOnlineCountAsync(roomId);

            await Clients.Group(roomId.ToString()).SendAsync("UserJoined", new
            {
                UserId = userId,
                UserNickname = user.Nickname,
                RoomId = roomId,
                OnlineCount = onlineCount,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} joined room {RoomId} (online: {OnlineCount})",
                userId, roomId, onlineCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room {RoomId} for user {UserId}", roomId, userId);
            throw new HubException($"Failed to join room: {ex.Message}");
        }
    }

    public async Task LeaveRoom(Guid roomId, Guid userId)
    {
        try
        {
            // Используем RoomService
            await _roomService.LeaveRoomAsync(userId, roomId);

            // Удаляем из группы SignalR
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

            // Отмечаем оффлайн
            await _onlineUsersService.MarkUserOfflineAsync(roomId, userId);

            // Получаем актуальный онлайн счетчик
            var onlineCount = await _onlineUsersService.GetOnlineCountAsync(roomId);

            await Clients.Group(roomId.ToString()).SendAsync("UserLeft", new
            {
                UserId = userId,
                RoomId = roomId,
                OnlineCount = onlineCount,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("User {UserId} left room {RoomId} (online: {OnlineCount})",
                userId, roomId, onlineCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving room {RoomId} for user {UserId}", roomId, userId);
            throw new HubException($"Failed to leave room: {ex.Message}");
        }
    }

    public async Task SendMessage(Guid roomId, string text, Guid userId)
    {
        try
        {
            // При отправке сообщения обновляем время последней активности
            await _onlineUsersService.MarkUserOnlineAsync(roomId, userId);

            // Отправляем сообщение
            var message = await _chatService.SendMessageAsync(userId, roomId, text);

            // Рассылаем всем в комнате
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);

            _logger.LogInformation("Message sent to room {RoomId} by user {UserId}", roomId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            throw new HubException($"Failed to send message: {ex.Message}");
        }
    }

    // Heartbeat для поддержания онлайн статуса
    public async Task Heartbeat(Guid roomId, Guid userId)
    {
        try
        {
            await _onlineUsersService.MarkUserOnlineAsync(roomId, userId);
            await _onlineUsersService.MarkUserOnlineGloballyAsync(userId);

            _logger.LogDebug("Heartbeat from user {UserId} in room {RoomId}", userId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in heartbeat");
        }
    }
}