namespace api.Services;

/// <summary>
/// Settings for AWS S3 file uploads
/// </summary>
public class FileUploadSettings
{
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public long MaxFileSize { get; set; } = 104857600; // 100MB default
    public string[] AllowedFileTypes { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov", ".avi" };
}
