using CloudinaryDotNet.Actions;

namespace API.Interfaces;

public interface IPhotoService
{
    Task<ImageUploadResult> UploadPhotoAsync(IFormFile file, CancellationToken ct = default);
    Task<DeletionResult> DeletePhotoAsync(string publicId, CancellationToken ct = default);
}
