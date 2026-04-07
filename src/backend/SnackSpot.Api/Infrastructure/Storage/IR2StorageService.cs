namespace SnackSpot.Api.Infrastructure.Storage;

public interface IR2StorageService
{
    Task<(string uploadUrl, string imageUrl)> GeneratePresignedPutUrlAsync(
        string fileName, string contentType, TimeSpan expiry);
}
