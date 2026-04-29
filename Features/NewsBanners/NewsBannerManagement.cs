using System.Text;
using MiniApp.Data;
using MiniApp.Features.Offers;

namespace MiniApp.Features.NewsBanners;

public static class NewsBannerManagement
{
    public const int RequiredImageWidth = 1080;
    public const int RequiredImageHeight = 540;
    public const long MaxUploadBytes = 1_000_000;
    public const string ActionTypeNone = "none";
    public const string ActionTypeAppSection = "app_section";
    public const string ActionTypeExternalUrl = "external_url";
    public const string ActionTypeDiscountedOffer = "discounted_offer";

    private static readonly HashSet<string> SupportedActionTypes = new(StringComparer.Ordinal)
    {
        ActionTypeNone,
        ActionTypeAppSection,
        ActionTypeExternalUrl,
        ActionTypeDiscountedOffer
    };

    private static readonly HashSet<string> SupportedAppSections = new(StringComparer.Ordinal)
    {
        "lottery",
        "tickets",
        "winners",
        "profile",
        "profile/deposit",
        "profile/invite",
        "profile/withdraw",
        "profile/history"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/pjpeg"
    };

    public static NewsBannerDto ToDto(NewsBanner banner, string? locale = null, DiscountedTicketOfferDto? offer = null)
    {
        var resolvedImagePath = ResolveImagePath(banner, locale);
        var normalizedType = NormalizeStoredActionType(banner.ActionType);
        if (normalizedType == ActionTypeDiscountedOffer)
        {
            if (offer is null)
            {
                return new NewsBannerDto(
                    banner.Id,
                    resolvedImagePath,
                    ActionTypeNone,
                    null);
            }

            return new NewsBannerDto(
                banner.Id,
                resolvedImagePath,
                ActionTypeDiscountedOffer,
                offer.Id.ToString(),
                offer);
        }

        return new NewsBannerDto(
            banner.Id,
            resolvedImagePath,
            normalizedType,
            NormalizeStoredActionValue(banner.ActionType, banner.ActionValue));
    }

    public static string ResolveImagePath(NewsBanner banner, string? locale)
    {
        var localizedPath = NormalizeImageLocale(locale) switch
        {
            "ru" => banner.ImagePathRu,
            "uz" => banner.ImagePathUz,
            "en" => banner.ImagePathEn,
            _ => null
        };

        return string.IsNullOrWhiteSpace(localizedPath)
            ? banner.ImagePath
            : localizedPath;
    }

    public static IReadOnlyList<string> GetAllImagePaths(NewsBanner banner)
        => new[]
        {
            banner.ImagePath,
            banner.ImagePathEn,
            banner.ImagePathRu,
            banner.ImagePathUz
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public static IReadOnlyList<string> GetSupportedAppSections()
        => SupportedAppSections.OrderBy(x => x, StringComparer.Ordinal).ToArray();

    public static string NormalizeStoredActionType(string? actionType)
    {
        var value = (actionType ?? string.Empty).Trim().ToLowerInvariant();
        return SupportedActionTypes.Contains(value) ? value : ActionTypeNone;
    }

    public static string? NormalizeStoredActionValue(string? actionType, string? actionValue)
    {
        var normalizedType = NormalizeStoredActionType(actionType);
        if (normalizedType == ActionTypeNone)
            return null;

        var trimmed = string.IsNullOrWhiteSpace(actionValue) ? null : actionValue.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static bool TryNormalizeAction(
        string? actionTypeInput,
        string? actionValueInput,
        out string normalizedActionType,
        out string? normalizedActionValue,
        out string validationErrorCode)
    {
        normalizedActionType = (actionTypeInput ?? string.Empty).Trim().ToLowerInvariant();
        normalizedActionValue = null;
        validationErrorCode = string.Empty;

        if (!SupportedActionTypes.Contains(normalizedActionType))
        {
            validationErrorCode = "invalid_type";
            return false;
        }

        var trimmedValue = (actionValueInput ?? string.Empty).Trim();
        if (normalizedActionType == ActionTypeNone)
        {
            normalizedActionValue = null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            validationErrorCode = "missing_value";
            return false;
        }

        if (normalizedActionType == ActionTypeAppSection)
        {
            var section = trimmedValue.ToLowerInvariant();
            if (!SupportedAppSections.Contains(section))
            {
                validationErrorCode = "invalid_app_section";
                return false;
            }

            normalizedActionValue = section;
            return true;
        }

        if (normalizedActionType == ActionTypeDiscountedOffer)
        {
            if (!long.TryParse(trimmedValue, out var offerId) || offerId <= 0)
            {
                validationErrorCode = "invalid_discounted_offer";
                return false;
            }

            normalizedActionValue = offerId.ToString();
            return true;
        }

        if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out var absoluteUri)
            || !string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            validationErrorCode = "invalid_external_url";
            return false;
        }

        normalizedActionValue = absoluteUri.ToString();
        return true;
    }

    public static string GetUploadsDirectory(IWebHostEnvironment env)
    {
        var webRoot = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;

        return Path.Combine(webRoot, "uploads", "news-banners");
    }

    public static async Task<NewsBannerImageSaveResult> SaveImageAsync(IFormFile? imageFile, IWebHostEnvironment env, CancellationToken ct)
    {
        if (imageFile is null || imageFile.Length <= 0)
            return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.MissingFile);

        if (imageFile.Length > MaxUploadBytes)
            return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.FileTooLarge);

        var extension = Path.GetExtension(imageFile.FileName);
        if (!AllowedExtensions.Contains(extension))
            return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.InvalidType);

        var contentType = imageFile.ContentType.Trim();
        if (contentType.Length > 0 && !AllowedContentTypes.Contains(contentType))
            return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.InvalidType);

        try
        {
            await using var imageStream = imageFile.OpenReadStream();
            if (!TryReadJpegDimensions(imageStream, out var width, out var height))
                return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.InvalidJpeg);

            if (width != RequiredImageWidth || height != RequiredImageHeight)
                return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.InvalidDimensions, width, height);

            Directory.CreateDirectory(GetUploadsDirectory(env));

            var fileName = $"{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(GetUploadsDirectory(env), fileName);

            await using var sourceStream = imageFile.OpenReadStream();
            await using var destinationStream = File.Create(filePath);
            await sourceStream.CopyToAsync(destinationStream, ct);

            return new NewsBannerImageSaveResult(true, GetPublicImagePath(fileName), NewsBannerImageError.None, width, height);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new NewsBannerImageSaveResult(false, null, NewsBannerImageError.SaveFailed);
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

    public static void DeleteImagesIfExists(IWebHostEnvironment env, IEnumerable<string?> publicImagePaths)
    {
        foreach (var publicImagePath in publicImagePaths
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.Ordinal))
        {
            DeleteImageIfExists(env, publicImagePath);
        }
    }

    private static string GetPublicImagePath(string fileName)
        => $"/uploads/news-banners/{fileName}";

    private static string? NormalizeImageLocale(string? locale)
    {
        var value = (locale ?? string.Empty).Trim().ToLowerInvariant();
        if (value.StartsWith("ru", StringComparison.Ordinal)) return "ru";
        if (value.StartsWith("uz", StringComparison.Ordinal)) return "uz";
        if (value.StartsWith("en", StringComparison.Ordinal)) return "en";
        return null;
    }

    private static bool TryReadJpegDimensions(Stream stream, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (!stream.CanRead)
            return false;

        if (stream.CanSeek)
            stream.Position = 0;

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (reader.ReadByte() != 0xFF || reader.ReadByte() != 0xD8)
                return false;

            while (true)
            {
                byte markerPrefix;
                do
                {
                    markerPrefix = reader.ReadByte();
                }
                while (markerPrefix != 0xFF);

                byte marker;
                do
                {
                    marker = reader.ReadByte();
                }
                while (marker == 0xFF);

                if (marker == 0xD9 || marker == 0xDA)
                    break;

                if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                    continue;

                var segmentLength = ReadBigEndianUInt16(reader);
                if (segmentLength < 2)
                    return false;

                if (IsStartOfFrameMarker(marker))
                {
                    if (segmentLength < 7)
                        return false;

                    _ = reader.ReadByte(); // sample precision
                    height = ReadBigEndianUInt16(reader);
                    width = ReadBigEndianUInt16(reader);
                    return width > 0 && height > 0;
                }

                var bytesToSkip = segmentLength - 2;
                var skipped = reader.ReadBytes(bytesToSkip);
                if (skipped.Length != bytesToSkip)
                    return false;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsStartOfFrameMarker(byte marker)
        => marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static int ReadBigEndianUInt16(BinaryReader reader)
        => (reader.ReadByte() << 8) | reader.ReadByte();
}


