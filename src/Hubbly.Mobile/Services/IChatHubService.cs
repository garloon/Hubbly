using Microsoft.AspNetCore.SignalR.Client;

namespace Hubbly.Mobile.Services;

public interface IChatHubService
{
    HubConnection? Connection { get; }
    Task ConnectAsync(string token);
    Task DisconnectAsync();
    Task JoinRoomAsync(Guid roomId);
    Task LeaveRoomAsync(Guid roomId);
    Task SendMessageAsync(Guid roomId, string text);
    Task StartTypingAsync(Guid roomId);
    Task StopTypingAsync(Guid roomId);
}

public class ChatHubService : IChatHubService
{
    private readonly ITokenStorage _tokenStorage;
    private HubConnection? _hubConnection;

    public HubConnection? Connection => _hubConnection;

    public ChatHubService(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public async Task ConnectAsync(string token)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://192.168.1.203:5081/chatHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task JoinRoomAsync(Guid roomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinRoom", roomId);
        }
    }

    public async Task LeaveRoomAsync(Guid roomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveRoom", roomId);
        }
    }

    public async Task SendMessageAsync(Guid roomId, string text)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMessage", roomId, text);
        }
    }

    public async Task StartTypingAsync(Guid roomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("Typing", roomId, true);
        }
    }

    public async Task StopTypingAsync(Guid roomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("Typing", roomId, false);
        }
    }
}