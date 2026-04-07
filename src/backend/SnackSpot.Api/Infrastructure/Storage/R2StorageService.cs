using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace SnackSpot.Api.Infrastructure.Storage;

public class R2StorageService(IConfiguration config) : IR2StorageService
{
    public async Task<(string uploadUrl, string imageUrl)> GeneratePresignedPutUrlAsync(
        string fileName, string contentType, TimeSpan expiry)
    {
        var accountId = config["R2:AccountId"] ?? throw new InvalidOperationException("R2:AccountId is not configured.");
        var bucket = config["R2:BucketName"] ?? throw new InvalidOperationException("R2:BucketName is not configured.");
        var accessKey = config["R2:AccessKeyId"] ?? throw new InvalidOperationException("R2:AccessKeyId is not configured.");
        var secretKey = config["R2:SecretAccessKey"] ?? throw new InvalidOperationException("R2:SecretAccessKey is not configured.");
        var publicUrl = config["R2:PublicUrl"] ?? throw new InvalidOperationException("R2:PublicUrl is not configured.");

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        using var client = new AmazonS3Client(credentials, s3Config);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = fileName,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = contentType
        };

        var uploadUrl = await client.GetPreSignedURLAsync(request);
        var imageUrl = $"{publicUrl.TrimEnd('/')}/{fileName}";

        return (uploadUrl, imageUrl);
    }
}
