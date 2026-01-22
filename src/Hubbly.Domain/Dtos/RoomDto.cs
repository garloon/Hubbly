using Hubbly.Domain.Enums;

namespace Hubbly.Domain.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoomType Type { get; set; }
    public int MaxUsers { get; set; }
    public int CurrentUsersCount { get; set; }
    public bool HasAvailableSpace => CurrentUsersCount < MaxUsers;
    public Guid? CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
}