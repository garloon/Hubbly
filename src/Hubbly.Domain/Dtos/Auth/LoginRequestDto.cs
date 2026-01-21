namespace Hubbly.Domain.Dtos.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}