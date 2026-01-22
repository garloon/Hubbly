using Hubbly.Mobile.Models;
using Hubbly.Mobile.Utils;

namespace Hubbly.Mobile.Services;

public class ApiService : IApiService
{
    private readonly IUsersApiService _usersApi;
    private readonly IRoomsApiService _roomsApi;
    private readonly IMessagesApiService _messagesApi;
    private readonly ITokenStorage _tokenStorage;
    private readonly HttpClient _httpClient;

    public ApiService(
        IUsersApiService usersApi,
        IRoomsApiService roomsApi,
        IMessagesApiService messagesApi,
        ITokenStorage tokenStorage,
        HttpClient httpClient)
    {
        _usersApi = usersApi;
        _roomsApi = roomsApi;
        _messagesApi = messagesApi;
        _tokenStorage = tokenStorage;
        _httpClient = httpClient;
    }

    public async Task<UserDto?> GetOrCreateGuestAsync(string deviceId, string nickname)
    {
        DebugLogger.Log($"GetOrCreateGuestAsync: deviceId={deviceId}, nickname={nickname}");

        try
        {
            // Используем прямой HTTP вызов для отладки
            var url = "http://localhost:5081/api/users/guest";
            var request = new CreateGuestRequest
            {
                DeviceId = deviceId,
                Nickname = nickname
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            DebugLogger.Log($"Request JSON: {json}");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            DebugLogger.Log($"Sending POST to {url}");
            var response = await client.PostAsync(url, content);

            DebugLogger.Log($"Response Status: {response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync();
            DebugLogger.Log($"Response Body: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                // Парсим ответ
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseBody, options);

                if (result?.Success == true && result.Data != null)
                {
                    DebugLogger.Log($"User created successfully: {result.Data.Id}");

                    await _tokenStorage.SaveUserIdAsync(result.Data.Id);
                    await _tokenStorage.SaveDeviceIdAsync(deviceId);

                    return result.Data;
                }
                else
                {
                    DebugLogger.Log($"API returned error: {result?.Error}");
                    return null;
                }
            }
            else
            {
                DebugLogger.Log($"HTTP Error: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Exception in GetOrCreateGuestAsync: {ex.Message}");
            DebugLogger.Log($"Stack: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<UserDto> GetCurrentUserAsync()
    {
        try
        {
            return await _usersApi.GetCurrentUserAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current user: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateNicknameAsync(string newNickname)
    {
        try
        {
            var response = await _usersApi.UpdateNicknameAsync(new UpdateNicknameRequest
            {
                NewNickname = newNickname
            });

            return response.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating nickname: {ex.Message}");
            return false;
        }
    }

    public async Task<RoomDto> GetAvailableSystemRoomAsync()
    {
        try
        {
            return await _roomsApi.GetAvailableSystemRoomAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting available room: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> JoinRoomAsync(Guid roomId)
    {
        try
        {
            var response = await _roomsApi.JoinRoomAsync(roomId);
            return !string.IsNullOrEmpty(response?.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error joining room: {ex.Message}");
            return false;
        }
    }

    public async Task<List<MessageDto>> GetRoomMessagesAsync(Guid roomId, int limit = 50)
    {
        try
        {
            var response = await _messagesApi.GetRoomMessagesAsync(roomId, limit);
            return response.Messages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting room messages: {ex.Message}");
            return new List<MessageDto>();
        }
    }

    public async Task<MessageDto> SendMessageAsync(Guid roomId, string text)
    {
        try
        {
            return await _messagesApi.SendMessageAsync(roomId, new SendMessageRequest
            {
                Text = text
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            throw;
        }
    }
}