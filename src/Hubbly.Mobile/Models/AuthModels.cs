namespace Hubbly.Mobile.Models;

public class CreateGuestRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
}

public class UpdateNicknameRequest
{
    public string NewNickname { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsGuest => string.IsNullOrEmpty(Email);
    public string? Email { get; set; }
    public Guid? LastRoomId { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public override string ToString()
    {
        return $"Success: {Success}, Error: {Error}";
    }
}