using Hubbly.Domain.DTOs;

namespace Hubbly.Domain.Interfaces;

public interface IUserService
{
    // Гостевая авторизация по deviceId
    Task<UserDto> GetOrCreateGuestUserAsync(string deviceId, string nickname);

    // Получить пользователя по ID
    Task<UserDto?> GetUserByIdAsync(Guid userId);

    // Обновить никнейм
    Task<UserDto> UpdateNicknameAsync(Guid userId, string newNickname);

    // Привязать email (регистрация)
    Task<UserDto> RegisterUserAsync(Guid userId, string email);

    // Обновить последнюю активность
    Task UpdateLastActivityAsync(Guid userId);

    // Очистка неактивных гостей (фоновый процесс)
    Task CleanupInactiveGuestsAsync(TimeSpan inactivityThreshold);
}