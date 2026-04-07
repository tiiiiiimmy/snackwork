namespace SnackSpot.Api.Models.DTOs.Messages;

public class SendMessageRequest
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}
