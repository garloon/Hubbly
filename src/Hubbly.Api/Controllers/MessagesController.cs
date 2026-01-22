using Hubbly.Domain.Dtos.Messages;
using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : BaseApiController
{
    private readonly IChatService _chatService;

    public MessagesController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> GetRoomMessages(
        Guid roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var messages = await _chatService.GetRoomMessagesAsync(roomId, CurrentUserId, page, pageSize);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get messages: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMessage(Guid id)
    {
        try
        {
            var message = await _chatService.GetMessageByIdAsync(id, CurrentUserId);
            return OkOrNotFound(message, "Message not found");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get message: {ex.Message}");
        }
    }

    [HttpPost("room/{roomId}")]
    public async Task<IActionResult> SendMessage(Guid roomId, [FromBody] SendMessageDto dto)
    {
        Console.WriteLine($"=== SendMessage called ===");
        Console.WriteLine($"RoomId: {roomId}");
        Console.WriteLine($"Text: {dto.Text}");

        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest(new { error = "Message text is required" });

        try
        {
            var message = await _chatService.SendMessageAsync(CurrentUserId, roomId, dto);

            // ЛОГИРУЕМ ОТВЕТ
            var response = new
            {
                message.Id,
                message.Text,
                message.SenderId,
                message.RoomId,
                message.Type,
                message.CreatedAt
            };

            Console.WriteLine($"Sending response: {System.Text.Json.JsonSerializer.Serialize(response)}");

            return Ok(message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to send message: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Text))
            return BadRequestWithError("Message text is required");

        try
        {
            var success = await _chatService.EditMessageAsync(CurrentUserId, id, dto.Text);

            if (success.Success)
                return Ok(new { message = "Message updated successfully" });

            return NotFound(new { error = "Message not found or cannot be edited" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to edit message: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        try
        {
            var success = await _chatService.DeleteMessageAsync(CurrentUserId, id);

            if (success.Success)
                return Ok(new { message = "Message deleted successfully" });

            return NotFound(new { error = "Message not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to delete message: {ex.Message}");
        }
    }
}