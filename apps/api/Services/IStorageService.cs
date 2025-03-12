namespace api.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder, string fileName);
    Task DeleteFileAsync(string fileUrl);
}