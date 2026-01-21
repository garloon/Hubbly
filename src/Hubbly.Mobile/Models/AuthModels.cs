namespace Hubbly.Mobile.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class SimpleResponse
{
    public string? Message { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public UserDto? User { get; set; }
    public string? Error { get; set; }
}