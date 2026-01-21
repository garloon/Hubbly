namespace Hubbly.Domain.Dtos.Rooms;
public class RoomDetailsDto : RoomDto
{
    public List<RoomMemberDto> Members { get; set; } = new();
    public bool HasPassword { get; set; }
    public string? InviteCode { get; set; }
    public DateTime? InviteCodeExpires { get; set; }
}
