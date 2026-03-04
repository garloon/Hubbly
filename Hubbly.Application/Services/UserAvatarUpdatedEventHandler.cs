using Hubbly.Domain.Dtos;
using Hubbly.Domain.Events;
using Hubbly.Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Hubbly.Application.Services;

public class UserAvatarUpdatedEventHandler : IDomainEventHandler<UserAvatarUpdatedEvent>
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserAvatarUpdatedEventHandler> _logger;

    public UserAvatarUpdatedEventHandler(
        IHubContext<ChatHub> hubContext,
        IUserRepository userRepository,
        ILogger<UserAvatarUpdatedEventHandler> logger)
    {
        _hubContext = hubContext;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task HandleAsync(UserAvatarUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserId"] = @event.UserId
        });

        _logger.LogDebug("Handling UserAvatarUpdatedEvent for user {UserId}", @event.UserId);

        try
        {
            // Get user's current room via LastRoomId
            var user = await _userRepository.GetByIdAsync(@event.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for avatar update broadcast", @event.UserId);
                return;
            }

            var roomId = user.LastRoomId;
            if (!roomId.HasValue)
            {
                _logger.LogDebug("User {UserId} is not in any room, skipping broadcast", @event.UserId);
                return;
            }

            // Create broadcast DTO
            var avatarData = new UserAvatarUpdatedData
            {
                UserId = @event.UserId.ToString(),
                Nickname = user.Nickname,
                AvatarConfigJson = @event.AvatarConfigJson
            };

            // Broadcast to all users in the room (including sender)
            await _hubContext.Clients.Group(roomId.Value.ToString())
                .SendAsync("UserAvatarUpdated", avatarData, cancellationToken);

            _logger.LogInformation("Broadcasted avatar update for user {UserId} to room {RoomId}",
                @event.UserId, roomId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting avatar update for user {UserId}", @event.UserId);
            throw;
        }
    }
}
