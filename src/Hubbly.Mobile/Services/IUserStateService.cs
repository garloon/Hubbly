namespace Hubbly.Mobile.Services;

public interface IUserStateService
{
    Task<Guid?> GetLastRoomIdAsync();
    Task SaveLastRoomIdAsync(Guid roomId);
    Task ClearLastRoomIdAsync();
    Task<string?> GetLastRoomTitleAsync();
    Task SaveLastRoomTitleAsync(string title);
}

public class UserStateService : IUserStateService
{
    private const string LastRoomIdKey = "last_room_id";
    private const string LastRoomTitleKey = "last_room_title";

    public async Task<Guid?> GetLastRoomIdAsync()
    {
        var roomIdStr = await SecureStorage.Default.GetAsync(LastRoomIdKey);

        if (string.IsNullOrEmpty(roomIdStr) || !Guid.TryParse(roomIdStr, out var roomId))
            return null;

        return roomId;
    }

    public async Task SaveLastRoomIdAsync(Guid roomId)
    {
        await SecureStorage.Default.SetAsync(LastRoomIdKey, roomId.ToString());
    }

    public async Task ClearLastRoomIdAsync()
    {
        SecureStorage.Default.Remove(LastRoomIdKey);
    }

    public async Task<string?> GetLastRoomTitleAsync()
    {
        return await SecureStorage.Default.GetAsync(LastRoomTitleKey);
    }

    public async Task SaveLastRoomTitleAsync(string title)
    {
        await SecureStorage.Default.SetAsync(LastRoomTitleKey, title);
    }
}