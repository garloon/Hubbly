namespace Hubbly.Domain.Dtos;

/// <summary>
/// Data about user assignment to a room
/// </summary>
public class RoomAssignmentData
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int UsersInRoom { get; set; }
    public int MaxUsers { get; set; }
}

/// <summary>
/// Data about the connected user
/// </summary>
public class UserJoinedData
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string AvatarConfigJson { get; set; } = "{}";
    public DateTimeOffset JoinedAt { get; set; }
}

/// <summary>
/// Data about the user who left
/// </summary>
public class UserLeftData
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public DateTimeOffset LeftAt { get; set; }
}

/// <summary>
/// Data about the typing user
/// </summary>
public class UserTypingData
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
}
