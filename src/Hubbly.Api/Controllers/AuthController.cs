using Hubbly.Domain.Dtos.Auth;
using Hubbly.Domain.Interfaces;
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
            return BadRequest(new { success = false, error = "Email is required" });
        }

        var result = await _authService.RequestLoginAsync(request.Email);

        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                message = "OTP code sent to email",
                timestamp = DateTime.UtcNow
            });
        }

        return BadRequest(new
        {
            success = false,
            error = result.Error
        });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        Console.WriteLine($"=== VerifyOtp called ===");
        Console.WriteLine($"Email: {request.Email}");
        Console.WriteLine($"OTP Code: {request.OtpCode}");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
        {
            Console.WriteLine($"Error: Email or OTP is empty");
            return BadRequest(new { error = "Email and OTP code are required" });
        }

        var result = await _authService.VerifyOtpAsync(request.Email, request.OtpCode);

        Console.WriteLine($"AuthService result: Success={result.Success}, Error={result.Error}");

        if (result.Success)
        {
            Console.WriteLine($"Verification successful for email: {request.Email}");
            return Ok(new
            {
                token = result.Token,
                user = result.User
            });
        }

        Console.WriteLine($"Verification failed: {result.Error}");
        return Unauthorized(new { error = result.Error });
    }

    [HttpGet("test-token")]
    [Authorize]
    public IActionResult TestToken()
    {
        return Ok(new
        {
            message = "Token is valid!",
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            timestamp = DateTime.UtcNow
        });
    }
}