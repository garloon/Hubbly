namespace Hubbly.Domain.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsGuest => string.IsNullOrEmpty(Email);
    public string? Email { get; set; }
    public Guid? LastRoomId { get; set; }
}