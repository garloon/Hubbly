using Hubbly.Domain.Enums;

namespace Hubbly.Domain.Dtos.Rooms;

public class CreateRoomDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoomType Type { get; set; } = RoomType.Public;
    public int MaxMembers { get; set; } = 0;
}
