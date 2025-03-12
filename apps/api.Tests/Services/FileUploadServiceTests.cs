using api.Services;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace api.Tests.Services;

public class FileUploadServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly Mock<IOptions<FileUploadSettings>> _settingsMock;
    private readonly Mock<ILogger<FileUploadService>> _loggerMock;
    private readonly FileUploadService _fileUploadService;

    public FileUploadServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _settingsMock = new Mock<IOptions<FileUploadSettings>>();
        _loggerMock = new Mock<ILogger<FileUploadService>>();

        _settingsMock.Setup(x => x.Value).Returns(new FileUploadSettings
        {
            BucketName = "test-bucket",
            Region = "us-east-1",
            PresignedUrlExpirationMinutes = 60
        });

        _fileUploadService = new FileUploadService(
            _s3ClientMock.Object,
            _settingsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GeneratePresignedUploadUrl_ValidInput_ReturnsUrl()
    {
        // Arrange
        var fileName = "test-file.jpg";
        var contentType = "image/jpeg";
        var userId = 1;
        var fileType = "profile";

        var expectedUrl = "https://test-bucket.s3.amazonaws.com/users/1/profile/test-file.jpg";

        _s3ClientMock.Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _fileUploadService.GeneratePresignedUploadUrlAsync(fileName, contentType, userId, fileType);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Presigned URL generated successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedUrl, result.Data.PresignedUrl);
        Assert.Contains($"users/{userId}/{fileType}/{fileName}", result.Data.FileKey);

        // Verify S3 client was called with correct parameters
        _s3ClientMock.Verify(x => x.GetPreSignedURLAsync(
            It.Is<GetPreSignedUrlRequest>(r => 
                r.BucketName == _settingsMock.Object.Value.BucketName && 
                r.Key.Contains($"users/{userId}/{fileType}/{fileName}") && 
                r.Verb == HttpVerb.PUT && 
                r.ContentType == contentType),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePresignedDownloadUrl_ValidInput_ReturnsUrl()
    {
        // Arrange
        var fileKey = "users/1/profile/test-file.jpg";
        var expectedUrl = "https://test-bucket.s3.amazonaws.com/users/1/profile/test-file.jpg";

        _s3ClientMock.Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _fileUploadService.GeneratePresignedDownloadUrlAsync(fileKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Presigned URL generated successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedUrl, result.Data.PresignedUrl);
        Assert.Equal(fileKey, result.Data.FileKey);

        // Verify S3 client was called with correct parameters
        _s3ClientMock.Verify(x => x.GetPreSignedURLAsync(
            It.Is<GetPreSignedUrlRequest>(r => 
                r.BucketName == _settingsMock.Object.Value.BucketName && 
                r.Key == fileKey && 
                r.Verb == HttpVerb.GET),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFile_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var fileKey = "users/1/profile/test-file.jpg";

        _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse
            {
                HttpStatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _fileUploadService.DeleteFileAsync(fileKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("File deleted successfully", result.Message);

        // Verify S3 client was called with correct parameters
        _s3ClientMock.Verify(x => x.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(r => 
                r.BucketName == _settingsMock.Object.Value.BucketName && 
                r.Key == fileKey),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFile_S3Error_ReturnsError()
    {
        // Arrange
        var fileKey = "users/1/profile/test-file.jpg";

        _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("S3 error"));

        // Act
        var result = await _fileUploadService.DeleteFileAsync(fileKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to delete file: S3 error", result.Message);
    }

    [Fact]
    public async Task CheckIfFileExists_FileExists_ReturnsTrue()
    {
        // Arrange
        var fileKey = "users/1/profile/test-file.jpg";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await _fileUploadService.CheckIfFileExistsAsync(fileKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("File exists", result.Message);
        Assert.True(result.Data.Exists);

        // Verify S3 client was called with correct parameters
        _s3ClientMock.Verify(x => x.GetObjectMetadataAsync(
            It.Is<GetObjectMetadataRequest>(r => 
                r.BucketName == _settingsMock.Object.Value.BucketName && 
                r.Key == fileKey),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckIfFileExists_FileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var fileKey = "users/1/profile/nonexistent-file.jpg";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("The specified key does not exist.")
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _fileUploadService.CheckIfFileExistsAsync(fileKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("File does not exist", result.Message);
        Assert.False(result.Data.Exists);
    }
}
