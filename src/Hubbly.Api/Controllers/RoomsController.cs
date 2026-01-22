using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : BaseApiController
{
    private readonly IRoomService _roomService;
    private readonly IUserService _userService;

    public RoomsController(
        IRoomService roomService,
        IUserService userService)
    {
        _roomService = roomService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRooms()
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get rooms: {ex.Message}");
        }
    }

    [HttpGet("available-system")]
    public async Task<IActionResult> GetAvailableSystemRoom()
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            var room = await _roomService.GetOrCreateAvailableSystemRoomAsync();
            return Ok(room);
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get available system room: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoomById(Guid id)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            return OkOrNotFound(room, "Room not found");
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get room: {ex.Message}");
        }
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinRoom(Guid id)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            var success = await _roomService.JoinRoomAsync(CurrentUserId!.Value, id);

            if (success)
            {
                // Обновляем last_room_id у пользователя
                await _userService.UpdateLastActivityAsync(CurrentUserId!.Value);

                return Ok(new
                {
                    message = "Successfully joined the room",
                    roomId = id
                });
            }

            return BadRequestWithError("Failed to join room. Room may be full or not found.");
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to join room: {ex.Message}");
        }
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveRoom(Guid id)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            var success = await _roomService.LeaveRoomAsync(CurrentUserId!.Value, id);

            if (success)
            {
                return Ok(new
                {
                    message = "Successfully left the room",
                    roomId = id
                });
            }

            return BadRequestWithError("Failed to leave room");
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to leave room: {ex.Message}");
        }
    }

    [HttpGet("{id}/users/count")]
    public async Task<IActionResult> GetRoomUsersCount(Guid id)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
                return NotFound(new { error = "Room not found" });

            return Ok(new
            {
                roomId = id,
                currentUsers = room.CurrentUsersCount,
                maxUsers = room.MaxUsers,
                hasSpace = room.HasAvailableSpace
            });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get room users count: {ex.Message}");
        }
    }
}