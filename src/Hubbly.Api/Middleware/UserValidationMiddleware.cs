using Hubbly.Domain.Interfaces;

namespace Hubbly.Api.Middleware;

public class UserValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserValidationMiddleware> _logger;

    public UserValidationMiddleware(
        RequestDelegate next,
        ILogger<UserValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Для SignalR пропускаем (там своя валидация)
        if (context.WebSockets.IsWebSocketRequest ||
            context.Request.Path.StartsWithSegments("/chatHub"))
        {
            await _next(context);
            return;
        }

        // Получаем IUserService из контейнера
        var userService = context.RequestServices.GetService<IUserService>();

        if (userService != null)
        {
            // Пытаемся получить userId из header
            if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var userId))
            {
                // Проверяем что пользователь существует
                var user = await userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    // Добавляем в контекст
                    context.Items["CurrentUserId"] = userId;
                    context.Items["CurrentUser"] = user;

                    _logger.LogDebug("User {UserId} authenticated via header", userId);
                }
                else
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                }
            }
        }

        await _next(context);
    }
}

// Extension метод остается
public static class UserValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseUserValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserValidationMiddleware>();
    }
}