using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Hubbly.Application.Features.Rooms;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userId, out var guid) ? guid : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?
        .FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => UserId.HasValue;
}
