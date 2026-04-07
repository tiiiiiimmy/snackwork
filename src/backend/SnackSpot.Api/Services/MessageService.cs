using Microsoft.EntityFrameworkCore;
using SnackSpot.Api.Common;
using SnackSpot.Api.Data;
using SnackSpot.Api.Models.DTOs.Messages;
using SnackSpot.Api.Models.Entities;

namespace SnackSpot.Api.Services;

public class MessageService(SnackSpotDbContext db) : IMessageService
{
    public async Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(Guid userId)
    {
        // All messages involving this user (not soft-deleted from their side)
        var sent = await db.Messages
            .Where(m => m.SenderId == userId && !m.IsDeletedBySender)
            .Include(m => m.Receiver)
            .ToListAsync();

        var received = await db.Messages
            .Where(m => m.ReceiverId == userId && !m.IsDeletedByReceiver)
            .Include(m => m.Sender)
            .ToListAsync();

        // Collect unique partner IDs
        var partnerIds = sent.Select(m => m.ReceiverId)
            .Union(received.Select(m => m.SenderId))
            .Distinct()
            .ToList();

        var conversations = new List<ConversationSummaryResponse>();
        foreach (var partnerId in partnerIds)
        {
            var allMessages = sent.Where(m => m.ReceiverId == partnerId)
                .Union(received.Where(m => m.SenderId == partnerId))
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            if (allMessages.Count == 0) continue;

            var latest = allMessages.First();
            var partner = latest.SenderId == userId ? latest.Receiver : latest.Sender;

            var unreadCount = received
                .Count(m => m.SenderId == partnerId && !m.IsRead);

            conversations.Add(new ConversationSummaryResponse
            {
                PartnerId = partnerId,
                PartnerUsername = partner.Username,
                PartnerAvatarUrl = partner.AvatarUrl,
                LastMessage = latest.Content,
                LastMessageAt = latest.CreatedAt,
                UnreadCount = unreadCount
            });
        }

        return conversations.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<PagedResponse<MessageResponse>> GetConversationAsync(
        Guid userId, Guid partnerId, int page, int pageSize)
    {
        var query = db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m =>
                (m.SenderId == userId && m.ReceiverId == partnerId && !m.IsDeletedBySender) ||
                (m.SenderId == partnerId && m.ReceiverId == userId && !m.IsDeletedByReceiver));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<MessageResponse>.Create(
            items.Select(ToResponse).ToList(), page, pageSize, total);
    }

    public async Task<MessageResponse> SendMessageAsync(SendMessageRequest req, Guid senderId)
    {
        // Sender must follow receiver
        var isFollowing = await db.Follows
            .AnyAsync(f => f.FollowerId == senderId && f.FollowingId == req.ReceiverId);
        if (!isFollowing)
            throw new UnauthorizedAccessException("You must follow a user before sending them a message");

        var receiver = await db.Users.FirstOrDefaultAsync(u => u.Id == req.ReceiverId && !u.IsDeleted)
            ?? throw new KeyNotFoundException("Recipient not found");

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = req.ReceiverId,
            Content = req.Content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var saved = await db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstAsync(m => m.Id == message.Id);

        return ToResponse(saved);
    }

    public async Task MarkReadAsync(Guid messageId, Guid requestingUserId)
    {
        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId)
            ?? throw new KeyNotFoundException("Message not found");

        if (message.ReceiverId != requestingUserId)
            throw new UnauthorizedAccessException("Only the receiver can mark a message as read");

        message.IsRead = true;
        await db.SaveChangesAsync();
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid requestingUserId)
    {
        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId)
            ?? throw new KeyNotFoundException("Message not found");

        if (message.SenderId == requestingUserId)
            message.IsDeletedBySender = true;
        else if (message.ReceiverId == requestingUserId)
            message.IsDeletedByReceiver = true;
        else
            throw new UnauthorizedAccessException("Cannot delete this message");

        await db.SaveChangesAsync();
    }

    public async Task<UnreadCountResponse> GetUnreadCountAsync(Guid userId)
    {
        var count = await db.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeletedByReceiver);

        return new UnreadCountResponse { Count = count };
    }

    // -- helpers --

    private static MessageResponse ToResponse(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderUsername = m.Sender?.Username ?? string.Empty,
        ReceiverId = m.ReceiverId,
        ReceiverUsername = m.Receiver?.Username ?? string.Empty,
        Content = m.Content,
        IsRead = m.IsRead,
        CreatedAt = m.CreatedAt
    };
}
