using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Hubbly.Api.Middleware;

public class UserMiddleware
{
    private readonly RequestDelegate _next;

    public UserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<Domain.Entities.User> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user != null)
                {
                    // Добавляем пользователя в контекст запроса
                    context.Items["CurrentUser"] = user;
                }
            }
        }

        await _next(context);
    }
}

// Extension метод для удобства
public static class UserMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserMiddleware>();
    }
}