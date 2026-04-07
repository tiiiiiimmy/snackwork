namespace SnackSpot.Api.Models.DTOs.Follow;

public class FollowStatusResponse
{
    public bool IsFollowing { get; set; }
    public bool IsFollowedBy { get; set; }
    public bool IsMutual { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}
