using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;

namespace api.Services;

/// <summary>
/// Service for handling file uploads to AWS S3
/// </summary>
public class FileUploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly FileUploadSettings _settings;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(
        IAmazonS3 s3Client,
        IOptions<FileUploadSettings> settings,
        ILogger<FileUploadService> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file to S3 and returns the URL
    /// </summary>
    public async Task<string> UploadFileAsync(IFormFile file, string folderName, string fileName = "")
    {
        try
        {
            // Generate a unique file name if not provided
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            }

            // Combine folder and file name for the S3 key
            string key = string.IsNullOrEmpty(folderName) 
                ? fileName 
                : $"{folderName.TrimEnd('/')}/{fileName}";

            // Upload file to S3
            using var fileStream = file.OpenReadStream();
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = key,
                BucketName = _settings.BucketName,
                CannedACL = S3CannedACL.PublicRead
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            // Generate the URL for the uploaded file
            string fileUrl = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{key}";
            
            _logger.LogInformation("File uploaded successfully to {FileUrl}", fileUrl);
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to S3", file.FileName);
            throw;
        }
    }

    /// <summary>
    /// Uploads a base64 encoded file to S3 and returns the URL
    /// </summary>
    public async Task<string> UploadBase64FileAsync(string base64String, string folderName, string fileName, string contentType)
    {
        try
        {
            // Remove data URL prefix if present
            if (base64String.Contains(","))
            {
                base64String = base64String.Split(',')[1];
            }

            // Convert base64 to byte array
            byte[] fileBytes = Convert.FromBase64String(base64String);

            // Combine folder and file name for the S3 key
            string key = string.IsNullOrEmpty(folderName) 
                ? fileName 
                : $"{folderName.TrimEnd('/')}/{fileName}";

            // Upload file to S3
            using var stream = new MemoryStream(fileBytes);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _settings.BucketName,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = contentType
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            // Generate the URL for the uploaded file
            string fileUrl = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{key}";
            
            _logger.LogInformation("Base64 file uploaded successfully to {FileUrl}", fileUrl);
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading base64 file to S3");
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from S3
    /// </summary>
    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            // Extract the key from the URL
            string key = fileUrl.Replace($"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/", "");

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("File deleted successfully from {FileUrl}", fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {FileUrl}", fileUrl);
            throw;
        }
    }

    /// <summary>
    /// Generates a pre-signed URL for direct upload to S3
    /// </summary>
    public string GeneratePresignedUploadUrl(string folderName, string fileName, string contentType, TimeSpan expiration)
    {
        try
        {
            // Combine folder and file name for the S3 key
            string key = string.IsNullOrEmpty(folderName) 
                ? fileName 
                : $"{folderName.TrimEnd('/')}/{fileName}";

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                ContentType = contentType,
                Expires = DateTime.UtcNow.Add(expiration)
            };

            string presignedUrl = _s3Client.GetPreSignedURL(request);
            _logger.LogInformation("Generated pre-signed URL for {Key}", key);
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed URL for {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Generates a pre-signed URL for downloading a file from S3
    /// </summary>
    public string GeneratePresignedDownloadUrl(string key, TimeSpan expiration)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expiration)
            };

            string presignedUrl = _s3Client.GetPreSignedURL(request);
            _logger.LogInformation("Generated pre-signed download URL for {Key}", key);
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed download URL for {Key}", key);
            throw;
        }
    }
}
