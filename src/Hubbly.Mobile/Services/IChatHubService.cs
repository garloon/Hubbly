using Hubbly.Mobile.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hubbly.Mobile.Services;

public interface IChatHubService
{
    HubConnection? Connection { get; }

    // События для UI
    event EventHandler<MessageDto> MessageReceived;
    event EventHandler<object> UserJoined;
    event EventHandler<object> UserLeft;
    event EventHandler<object> UserTyping;

    // Методы
    Task ConnectAsync(string deviceId, Guid userId);
    Task DisconnectAsync();
    Task JoinRoomAsync(Guid roomId, Guid userId);
    Task LeaveRoomAsync(Guid roomId, Guid userId);
    Task SendMessageAsync(Guid roomId, string text, Guid userId);
    Task StartTypingAsync(Guid roomId, Guid userId);
    Task StopTypingAsync(Guid roomId, Guid userId);
}