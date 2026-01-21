using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    protected string CurrentUserEmail =>
        User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

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
}