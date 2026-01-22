using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IValidator<string> _nicknameValidator;

    public UsersController(
        IUserService userService,
        IValidator<string> nicknameValidator)
    {
        _userService = userService;
        _nicknameValidator = nicknameValidator;
    }

    [HttpPost("guest")]
    public async Task<IActionResult> CreateGuestUser([FromBody] CreateGuestRequest request)
    {
        try
        {
            // Валидируем deviceId
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return BadRequestWithError("DeviceId is required");

            // Валидируем nickname
            var nicknameValidation = await _nicknameValidator.ValidateAsync(request.Nickname);
            if (!nicknameValidation.IsValid)
                return BadRequest(new { errors = nicknameValidation.Errors });

            // Создаем или получаем гостя
            var user = await _userService.GetOrCreateGuestUserAsync(
                request.DeviceId,
                request.Nickname);

            return ApiSuccess(user, "Guest user created successfully");
        }
        catch (Exception ex)
        {
            return ApiError($"Failed to create guest user: {ex.Message}");
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        return Ok(CurrentUser);
    }

    [HttpPut("nickname")]
    public async Task<IActionResult> UpdateNickname([FromBody] UpdateNicknameRequest request)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            // Валидируем новый nickname
            var validation = await _nicknameValidator.ValidateAsync(request.NewNickname);
            if (!validation.IsValid)
                return BadRequest(new { errors = validation.Errors });

            var updatedUser = await _userService.UpdateNicknameAsync(
                CurrentUserId!.Value,
                request.NewNickname);

            return Ok(new
            {
                user = updatedUser,
                message = "Nickname updated successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to update nickname: {ex.Message}");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterWithEmail([FromBody] RegisterRequest request)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequestWithError("Email is required");

            if (!IsValidEmail(request.Email))
                return BadRequestWithError("Invalid email format");

            var registeredUser = await _userService.RegisterUserAsync(
                CurrentUserId!.Value,
                request.Email);

            return Ok(new
            {
                user = registeredUser,
                message = "Email registered successfully"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to register email: {ex.Message}");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// DTOs для этого контроллера
public class CreateGuestRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
}

public class UpdateNicknameRequest
{
    public string NewNickname { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
}