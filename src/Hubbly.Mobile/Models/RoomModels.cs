namespace Hubbly.Mobile.Models;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Type { get; set; } // 1=System, 2=Public, 3=Private
    public int MaxUsers { get; set; }
    public int CurrentUsersCount { get; set; }
    public bool HasAvailableSpace => CurrentUsersCount < MaxUsers;
    public Guid? CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JoinRoomResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid RoomId { get; set; }
}