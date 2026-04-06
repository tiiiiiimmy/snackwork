namespace SnackSpot.Api.Models.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeletedBySender { get; set; } = false;
    public bool IsDeletedByReceiver { get; set; } = false;

    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
