namespace Hubbly.Application.Common.Models;

public static class CacheKeys
{
    // Пользователи
    public static string UserByDevice(string deviceId) => $"user_device_{deviceId}";
    public static string UserById(Guid userId) => $"user_{userId}";

    // Комнаты
    public static string RoomById(Guid roomId) => $"room_{roomId}";
    public static string AvailableSystemRooms => "available_system_rooms";

    // Сообщения
    public static string RecentMessages(Guid roomId, int count) => $"recent_messages_{roomId}_{count}";

    // Rate limiting
    public static string UserMessageRate(Guid userId) => $"rate_user_{userId}";
    public static string IpMessageRate(string ip) => $"rate_ip_{ip}";
}