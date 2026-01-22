using Hubbly.Mobile.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hubbly.Mobile.Services;

public class ChatHubService : IChatHubService
{
    private readonly ITokenStorage _tokenStorage;
    private HubConnection? _hubConnection;

    public HubConnection? Connection => _hubConnection;

    public ChatHubService(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public async Task ConnectAsync(string deviceId, Guid userId)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://192.168.1.203:5081/chatHub", options =>
            {
                // Передаем deviceId и userId в headers
                options.Headers["X-Device-Id"] = deviceId;
                options.Headers["X-User-Id"] = userId.ToString();
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
            .Build();

        // Подписываемся на события
        SetupEventHandlers();

        try
        {
            await _hubConnection.StartAsync();
            Console.WriteLine($"SignalR connected: {_hubConnection.State}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to SignalR: {ex.Message}");
            _hubConnection = null;
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting SignalR: {ex.Message}");
            }
            finally
            {
                _hubConnection = null;
            }
        }
    }

    public async Task JoinRoomAsync(Guid roomId, Guid userId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("JoinRoom", roomId, userId);
                Console.WriteLine($"Joined room {roomId} via SignalR");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining room via SignalR: {ex.Message}");
                throw;
            }
        }
    }

    public async Task LeaveRoomAsync(Guid roomId, Guid userId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("LeaveRoom", roomId, userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leaving room via SignalR: {ex.Message}");
            }
        }
    }

    public async Task SendMessageAsync(Guid roomId, string text, Guid userId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("SendMessage", roomId, text, userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message via SignalR: {ex.Message}");
                throw;
            }
        }
    }

    public async Task StartTypingAsync(Guid roomId, Guid userId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("Typing", roomId, userId, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting typing: {ex.Message}");
            }
        }
    }

    public async Task StopTypingAsync(Guid roomId, Guid userId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("Typing", roomId, userId, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping typing: {ex.Message}");
            }
        }
    }

    private void SetupEventHandlers()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<MessageDto>("ReceiveMessage", (message) =>
        {
            MessageReceived?.Invoke(this, message);
        });

        _hubConnection.On<object>("UserJoined", (data) =>
        {
            UserJoined?.Invoke(this, data);
        });

        _hubConnection.On<object>("UserLeft", (data) =>
        {
            UserLeft?.Invoke(this, data);
        });

        _hubConnection.On<object>("UserTyping", (data) =>
        {
            UserTyping?.Invoke(this, data);
        });

        // Ошибка соединения
        _hubConnection.Closed += async (error) =>
        {
            Console.WriteLine($"SignalR connection closed: {error?.Message}");
            await Task.Delay(2000);

            // Пытаемся переподключиться
            try
            {
                var deviceId = await _tokenStorage.GetDeviceIdAsync();
                var userId = await _tokenStorage.GetUserIdAsync();

                if (!string.IsNullOrEmpty(deviceId) && userId.HasValue)
                {
                    await ConnectAsync(deviceId, userId.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reconnect: {ex.Message}");
            }
        };
    }

    // События для UI
    public event EventHandler<MessageDto>? MessageReceived;
    public event EventHandler<object>? UserJoined;
    public event EventHandler<object>? UserLeft;
    public event EventHandler<object>? UserTyping;
}
