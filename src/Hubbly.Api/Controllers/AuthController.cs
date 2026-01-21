using Hubbly.Application.Common.Interfaces;
using Hubbly.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("request-login")]
    public async Task<IActionResult> RequestLogin([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var result = await _authService.RequestLoginAsync(request.Email);

        if (result.Success)
        {
            return Ok(new { message = "OTP code sent to email" });
        }

        return BadRequest(new { error = result.Error });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return BadRequest(new { error = "Email and OTP code are required" });
        }

        var result = await _authService.VerifyOtpAsync(request.Email, request.OtpCode);

        if (result.Success)
        {
            return Ok(new
            {
                token = result.Token,
                user = result.User
            });
        }

        return Unauthorized(new { error = result.Error });
    }

    [HttpGet("test-protected")]
    [Authorize] // Требует аутентификации
    public IActionResult TestProtected()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new
        {
            message = "You are authenticated!",
            userId,
            email,
            timestamp = DateTime.UtcNow
        });
    }
}