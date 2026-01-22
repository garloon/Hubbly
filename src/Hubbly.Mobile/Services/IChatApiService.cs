using Hubbly.Mobile.Models.Shared;
using Refit;
using System.Text.Json.Serialization;

namespace Hubbly.Mobile.Services;

public interface IChatApiService
{
    // Комнаты
    [Get("/api/rooms/public")]
    Task<List<RoomDto>> GetPublicRoomsAsync();

    [Get("/api/rooms/novice")]
    Task<RoomDto> GetNoviceRoomAsync();

    [Get("/api/rooms/{id}")]
    Task<RoomDto> GetRoomAsync(Guid id);

    [Post("/api/rooms/{id}/join")]
    Task<object> JoinRoomAsync(Guid id);

    // Сообщения
    [Get("/api/messages/room/{roomId}")]
    Task<string> GetRoomMessagesJsonAsync(Guid roomId, [Query] int page = 1, [Query] int pageSize = 50);

    [Post("/api/messages/room/{roomId}")]
    Task<string> SendMessageJsonAsync(Guid roomId, [Body] object request);
}

// Упрощенный DTO для отправки
public class SendMessageRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("replyToMessageId")]
    public Guid? ReplyToMessageId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; } = 1; // Text = 1
}