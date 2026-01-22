using Hubbly.Mobile.Models;
using Refit;

namespace Hubbly.Mobile.Services;

public interface IUsersApiService
{
    [Post("/api/users/guest")]
    Task<Models.ApiResponse<UserDto>> CreateGuestAsync([Body] CreateGuestRequest request);

    [Get("/api/users/me")]
    Task<UserDto> GetCurrentUserAsync();

    [Put("/api/users/nickname")]
    Task<Models.ApiResponse<UserDto>> UpdateNicknameAsync([Body] UpdateNicknameRequest request);
}

public interface IRoomsApiService
{
    [Get("/api/rooms")]
    Task<List<RoomDto>> GetAllRoomsAsync();

    [Get("/api/rooms/available-system")]
    Task<RoomDto> GetAvailableSystemRoomAsync();

    [Get("/api/rooms/{id}")]
    Task<RoomDto> GetRoomByIdAsync(Guid id);

    [Post("/api/rooms/{id}/join")]
    Task<JoinRoomResponse> JoinRoomAsync(Guid id);

    [Get("/api/rooms/{id}/users/count")]
    Task<object> GetRoomUsersCountAsync(Guid id);
}

public interface IMessagesApiService
{
    [Get("/api/messages/room/{roomId}")]
    Task<MessagesResponse> GetRoomMessagesAsync(Guid roomId, [Query] int limit = 50);

    [Post("/api/messages/room/{roomId}")]
    Task<MessageDto> SendMessageAsync(Guid roomId, [Body] SendMessageRequest request);

    [Get("/api/messages/room/{roomId}/recent")]
    Task<MessagesResponse> GetRecentMessagesAsync(Guid roomId, [Query] int count = 20);
}

// Основной сервис для удобства
public interface IApiService
{
    Task<UserDto?> GetOrCreateGuestAsync(string deviceId, string nickname);
    Task<UserDto> GetCurrentUserAsync();
    Task<bool> UpdateNicknameAsync(string newNickname);
    Task<RoomDto> GetAvailableSystemRoomAsync();
    Task<bool> JoinRoomAsync(Guid roomId);
    Task<List<MessageDto>> GetRoomMessagesAsync(Guid roomId, int limit = 50);
    Task<MessageDto> SendMessageAsync(Guid roomId, string text);
}