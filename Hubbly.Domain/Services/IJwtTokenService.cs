using System.Security.Claims;

namespace Hubbly.Domain.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string nickname);
    string GenerateRefreshToken();
    bool ValidateAccessToken(string token, out ClaimsPrincipal? principal);
    Guid? GetUserIdFromToken(string token);
}
