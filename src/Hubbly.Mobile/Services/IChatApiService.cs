using Hubbly.Mobile.Models.Shared;
using Refit;

namespace Hubbly.Mobile.Services;

public interface IChatApiService
{
    [Get("/api/rooms/public")]
    Task<List<RoomDto>> GetPublicRoomsAsync();
}