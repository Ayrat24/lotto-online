using MiniApp.Data;

namespace MiniApp.Features.Winners;

public static class WinnerManagement
{
    public const long MaxUploadBytes = 2_000_000;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    public static WinnerEntryDto ToDto(WinnerEntry entry)
        => new(entry.Id, entry.Name, entry.WinningAmountText, entry.QuoteText, entry.PhotoPath);

    public static string GetUploadsDirectory(IWebHostEnvironment env)
    {
        var webRoot = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;

        return Path.Combine(webRoot, "uploads", "winners");
    }

    public static async Task<WinnerImageSaveResult> SaveImageAsync(IFormFile? imageFile, IWebHostEnvironment env, CancellationToken ct)
    {
        if (imageFile is null || imageFile.Length <= 0)
            return new WinnerImageSaveResult(false, null, WinnerImageError.MissingFile);

        if (imageFile.Length > MaxUploadBytes)
            return new WinnerImageSaveResult(false, null, WinnerImageError.FileTooLarge);

        var extension = Path.GetExtension(imageFile.FileName ?? string.Empty);
        if (!AllowedExtensions.Contains(extension))
            return new WinnerImageSaveResult(false, null, WinnerImageError.InvalidType);

        var contentType = imageFile.ContentType?.Trim() ?? string.Empty;
        if (contentType.Length > 0 && !AllowedContentTypes.Contains(contentType))
            return new WinnerImageSaveResult(false, null, WinnerImageError.InvalidType);

        try
        {
            Directory.CreateDirectory(GetUploadsDirectory(env));

            var normalizedExtension = string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                ? ".jpg"
                : extension.ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{normalizedExtension}";
            var filePath = Path.Combine(GetUploadsDirectory(env), fileName);

            await using var sourceStream = imageFile.OpenReadStream();
            await using var destinationStream = File.Create(filePath);
            await sourceStream.CopyToAsync(destinationStream, ct);

            return new WinnerImageSaveResult(true, GetPublicImagePath(fileName), WinnerImageError.None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new WinnerImageSaveResult(false, null, WinnerImageError.SaveFailed);
        }
    }

    public static void DeleteImageIfExists(IWebHostEnvironment env, string? publicImagePath)
    {
        if (string.IsNullOrWhiteSpace(publicImagePath))
            return;

        var fileName = Path.GetFileName(publicImagePath.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var physicalPath = Path.Combine(GetUploadsDirectory(env), fileName);
        if (!File.Exists(physicalPath))
            return;

        try
        {
            File.Delete(physicalPath);
        }
        catch
        {
            // Ignore best-effort cleanup failures.
        }
    }

    private static string GetPublicImagePath(string fileName)
        => $"/uploads/winners/{fileName}";
}


