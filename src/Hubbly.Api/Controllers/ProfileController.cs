using Hubbly.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly UserManager<Domain.Entities.User> _userManager;

    public ProfileController(UserManager<Domain.Entities.User> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio
        });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Обновляем поля
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            user.Bio = request.Bio;
        }

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return Ok(new { message = "Profile updated successfully" });
        }

        return BadRequest(new
        {
            error = "Failed to update profile",
            details = result.Errors.Select(e => e.Description)
        });
    }
}

public class UpdateProfileDto
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}