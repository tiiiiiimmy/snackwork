using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Messages;
using SnackSpot.Api.Services;

namespace SnackSpot.Api.Controllers.V1;

[ApiController]
[Route("api/v1/messages")]
[Authorize]
public class MessagesController(IMessageService messageService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await messageService.GetConversationsAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await messageService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{partnerId:guid}")]
    public async Task<IActionResult> GetConversation(
        Guid partnerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await messageService.GetConversationAsync(userId, partnerId, page, pageSize);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
    {
        var senderId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await messageService.SendMessageAsync(req, senderId);
        return StatusCode(201, ApiResponse<object>.Ok(result));
    }

    [HttpPut("{messageId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid messageId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await messageService.MarkReadAsync(messageId, userId);
        return Ok(ApiResponse.Ok("Message marked as read"));
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await messageService.DeleteMessageAsync(messageId, userId);
        return Ok(ApiResponse.Ok("Message deleted"));
    }
}
