using Hubbly.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid? CurrentUserId =>
        HttpContext.Items["CurrentUserId"] as Guid?;

    protected UserDto? CurrentUser =>
        HttpContext.Items["CurrentUser"] as UserDto;

    protected IActionResult RequireUser()
    {
        if (!CurrentUserId.HasValue || CurrentUser == null)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }
        return null;
    }

    protected IActionResult OkOrNotFound<T>(T? result, string? message = null)
    {
        if (result == null)
        {
            return NotFound(new { error = message ?? "Resource not found" });
        }
        return Ok(result);
    }

    protected IActionResult BadRequestWithError(string error)
    {
        return BadRequest(new { error });
    }

    protected IActionResult ApiSuccess<T>(T data, string message = null)
    {
        return Ok(new
        {
            success = true,
            data = data,
            message = message
        });
    }

    protected IActionResult ApiError(string error, int statusCode = 400)
    {
        return StatusCode(statusCode, new
        {
            success = false,
            error = error
        });
    }
}