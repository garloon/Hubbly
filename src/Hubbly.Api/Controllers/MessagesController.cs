using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hubbly.Api.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : BaseApiController
{
    private readonly IChatService _chatService;
    private readonly IRoomService _roomService;

    public MessagesController(
        IChatService chatService,
        IRoomService roomService)
    {
        _chatService = chatService;
        _roomService = roomService;
    }

    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> GetRoomMessages(
        Guid roomId,
        [FromQuery] int limit = 50)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            // Проверяем что пользователь в комнате
            var isInRoom = await _roomService.IsUserInRoomAsync(CurrentUserId!.Value, roomId);
            if (!isInRoom)
                return Unauthorized(new { error = "User is not in room" });

            // Получаем историю (только по запросу)
            var messages = await _chatService.GetRoomHistoryAsync(roomId, limit);

            return Ok(new
            {
                roomId,
                messages,
                count = messages.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get messages: {ex.Message}");
        }
    }

    [HttpPost("room/{roomId}")]
    public async Task<IActionResult> SendMessage(
        Guid roomId,
        [FromBody] SendMessageRequest request)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequestWithError("Message text is required");

            // Проверяем что пользователь в комнате
            var isInRoom = await _roomService.IsUserInRoomAsync(CurrentUserId!.Value, roomId);
            if (!isInRoom)
            {
                // Пытаемся добавить
                var joined = await _roomService.JoinRoomAsync(CurrentUserId!.Value, roomId);
                if (!joined)
                    return BadRequestWithError("Cannot send message: not in room and cannot join");
            }

            var message = await _chatService.SendMessageAsync(
                CurrentUserId!.Value,
                roomId,
                request.Text);

            return Ok(new
            {
                message,
                success = true
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return StatusCode(429, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to send message: {ex.Message}");
        }
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(
        Guid messageId,
        [FromQuery] string? reason = null)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            // TODO: Проверять права (админ или создатель сообщения)
            var success = await _chatService.DeleteMessageAsync(messageId, reason);

            if (success)
                return Ok(new { message = "Message deleted successfully" });

            return NotFound(new { error = "Message not found or already deleted" });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to delete message: {ex.Message}");
        }
    }

    [HttpGet("room/{roomId}/recent")]
    public async Task<IActionResult> GetRecentMessages(
        Guid roomId,
        [FromQuery] int count = 20)
    {
        var requireUser = RequireUser();
        if (requireUser != null) return requireUser;

        try
        {
            // Проверяем что пользователь в комнате
            var isInRoom = await _roomService.IsUserInRoomAsync(CurrentUserId!.Value, roomId);
            if (!isInRoom)
                return Unauthorized(new { error = "User is not in room" });

            var messages = await _chatService.GetRecentMessagesAsync(roomId, count);

            return Ok(new
            {
                roomId,
                messages,
                count = messages.Count,
                isLive = true
            });
        }
        catch (Exception ex)
        {
            return BadRequestWithError($"Failed to get recent messages: {ex.Message}");
        }
    }
}

// DTO для отправки сообщения
public class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
}