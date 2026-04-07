namespace SnackSpot.Api.Models.DTOs.Reviews;

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
