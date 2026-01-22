namespace Hubbly.Mobile.Services;

public interface ITokenStorage
{
    // Токены (пока не используем JWT)
    Task<string?> GetAuthTokenAsync();
    Task SaveAuthTokenAsync(string token);
    Task DeleteAuthTokenAsync();

    // User ID
    Task<Guid?> GetUserIdAsync();
    Task SaveUserIdAsync(Guid userId);
    Task DeleteUserIdAsync();

    // Device ID
    Task<string?> GetDeviceIdAsync();
    Task SaveDeviceIdAsync(string deviceId);
    Task DeleteDeviceIdAsync();

    // Последняя комната
    Task<Guid?> GetLastRoomIdAsync();
    Task SaveLastRoomIdAsync(Guid roomId);
    Task ClearLastRoomIdAsync();

    // Проверка аутентификации
    Task<bool> IsAuthenticatedAsync();
}