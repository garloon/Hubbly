namespace Hubbly.Mobile.Services;

public class SecureStorageTokenService : ITokenStorage
{
    private const string AuthTokenKey = "auth_token";
    private const string UserIdKey = "user_id";
    private const string DeviceIdKey = "device_id";
    private const string LastRoomIdKey = "last_room_id";

    // Токены
    public async Task<string?> GetAuthTokenAsync()
    {
        return await SecureStorage.Default.GetAsync(AuthTokenKey);
    }

    public async Task SaveAuthTokenAsync(string token)
    {
        await SecureStorage.Default.SetAsync(AuthTokenKey, token);
    }

    public async Task DeleteAuthTokenAsync()
    {
        SecureStorage.Default.Remove(AuthTokenKey);
        await Task.CompletedTask;
    }

    // User ID
    public async Task<Guid?> GetUserIdAsync()
    {
        var userIdStr = await SecureStorage.Default.GetAsync(UserIdKey);
        return Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }

    public async Task SaveUserIdAsync(Guid userId)
    {
        await SecureStorage.Default.SetAsync(UserIdKey, userId.ToString());
    }

    public async Task DeleteUserIdAsync()
    {
        SecureStorage.Default.Remove(UserIdKey);
        await Task.CompletedTask;
    }

    // Device ID
    public async Task<string?> GetDeviceIdAsync()
    {
        return await SecureStorage.Default.GetAsync(DeviceIdKey);
    }

    public async Task SaveDeviceIdAsync(string deviceId)
    {
        await SecureStorage.Default.SetAsync(DeviceIdKey, deviceId);
    }

    public async Task DeleteDeviceIdAsync()
    {
        SecureStorage.Default.Remove(DeviceIdKey);
        await Task.CompletedTask;
    }

    // Последняя комната
    public async Task<Guid?> GetLastRoomIdAsync()
    {
        var roomIdStr = await SecureStorage.Default.GetAsync(LastRoomIdKey);
        return Guid.TryParse(roomIdStr, out var roomId) ? roomId : null;
    }

    public async Task SaveLastRoomIdAsync(Guid roomId)
    {
        await SecureStorage.Default.SetAsync(LastRoomIdKey, roomId.ToString());
    }

    public async Task ClearLastRoomIdAsync()
    {
        SecureStorage.Default.Remove(LastRoomIdKey);
        await Task.CompletedTask;
    }

    // Проверка аутентификации
    public async Task<bool> IsAuthenticatedAsync()
    {
        var userId = await GetUserIdAsync();
        return userId.HasValue;
    }
}
