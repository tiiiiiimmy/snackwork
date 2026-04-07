namespace SnackSpot.Api.Models.DTOs.Messages;

public class ConversationSummaryResponse
{
    public Guid PartnerId { get; set; }
    public string PartnerUsername { get; set; } = string.Empty;
    public string? PartnerAvatarUrl { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
