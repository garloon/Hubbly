using Hubbly.Domain.Dtos.Auth;

namespace Hubbly.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RequestLoginAsync(string email);
    Task<AuthResponseDto> VerifyOtpAsync(string email, string otpCode);
    Task<string> GenerateJwtTokenAsync(Domain.Entities.User user);
    Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken);
}