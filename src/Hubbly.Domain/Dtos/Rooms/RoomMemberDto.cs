namespace Hubbly.Domain.Dtos.Rooms;

public class RoomMemberDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsCreator { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
