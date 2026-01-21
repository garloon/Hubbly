using Hubbly.Domain.Dtos.Rooms;
using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : BaseApiController
{
    private readonly IRoomService _roomService;
    private readonly ICurrentUserService _currentUserService;

    public RoomsController(
        IRoomService roomService,
        ICurrentUserService currentUserService)
    {
        _roomService = roomService;
        _currentUserService = currentUserService;
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var rooms = await _roomService.GetPublicRoomsAsync(_currentUserService.UserId.Value, page, pageSize);
        return Ok(rooms);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRooms()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var rooms = await _roomService.GetUserRoomsAsync(_currentUserService.UserId.Value);
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var room = await _roomService.GetRoomByIdAsync(id, _currentUserService.UserId);
        return OkOrNotFound(room, "Room not found");
    }

    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetRoomDetails(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        try
        {
            var roomDetails = await _roomService.GetRoomDetailsAsync(id, _currentUserService.UserId.Value);
            return OkOrNotFound(roomDetails, "Room not found or access denied");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto request)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequestWithError("Title is required");

        try
        {
            var room = await _roomService.CreateRoomAsync(_currentUserService.UserId.Value, request);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to create room: {ex.Message}");
        }
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinRoom(Guid id, [FromBody] JoinRoomRequest? request = null)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var success = await _roomService.JoinRoomAsync(
            _currentUserService.UserId.Value,
            id,
            request?.InviteCode);

        if (success)
            return Ok(new { message = "Successfully joined the room" });

        return BadRequestWithError("Failed to join room. Room may be full, private, or invite code is invalid.");
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveRoom(Guid id)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var success = await _roomService.LeaveRoomAsync(_currentUserService.UserId.Value, id);

        if (success)
            return Ok(new { message = "Successfully left the room" });

        return BadRequestWithError("Failed to leave room. You may be the creator or not a member.");
    }
}

public class JoinRoomRequest
{
    public string? InviteCode { get; set; }
}