using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace api.Services;

public class S3StoragesService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StoragesService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:S3:BucketName"] ?? "rallycasts-media";
    }
    
    public async Task<string> UploadFileAsync(IFormFile file, string folder, string fileName) 
    {
        var key = $"{folder}/{fileName}";
        
        using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType
        };
        
        await _s3Client.PutObjectAsync(request);
        
        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }
    
    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;
        
        var uri = new Uri(fileUrl);
        var key = uri.AbsolutePath.TrimStart('/');
        
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };
        
        await _s3Client.DeleteObjectAsync(request);
    }
}