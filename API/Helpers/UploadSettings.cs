namespace API.Helpers;

public class UploadSettings
{
    public long MaxPhotoBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedPhotoMimeTypes { get; set; } = ["image/jpeg", "image/png", "image/webp"];
}
