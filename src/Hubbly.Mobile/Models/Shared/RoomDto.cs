namespace Hubbly.Mobile.Models.Shared;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string? CreatorAvatarUrl { get; set; }
    public int MemberCount { get; set; }
    public int OnlineCount { get; set; }
    public int MaxMembers { get; set; }
    public bool IsMember { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}