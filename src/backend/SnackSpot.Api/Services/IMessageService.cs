using SnackSpot.Api.Common;
using SnackSpot.Api.Models.DTOs.Messages;

namespace SnackSpot.Api.Services;

public interface IMessageService
{
    Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(Guid userId);
    Task<PagedResponse<MessageResponse>> GetConversationAsync(Guid userId, Guid partnerId, int page, int pageSize);
    Task<MessageResponse> SendMessageAsync(SendMessageRequest req, Guid senderId);
    Task MarkReadAsync(Guid messageId, Guid requestingUserId);
    Task DeleteMessageAsync(Guid messageId, Guid requestingUserId);
    Task<UnreadCountResponse> GetUnreadCountAsync(Guid userId);
}
