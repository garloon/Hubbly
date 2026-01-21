using Hubbly.Domain.Enums;

namespace Hubbly.Domain.Dtos.Rooms;

public class UpdateRoomDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public RoomType? Type { get; set; }
    public int? MaxMembers { get; set; }
}
