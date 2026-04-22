namespace MiniApp.Features.NewsBanners;

public sealed record NewsBannerDto(long Id, string ImageUrl, string ActionType, string? ActionValue);

public sealed record NewsBannersListResult(bool Ok, IReadOnlyList<NewsBannerDto> Banners);

public enum NewsBannerImageError
{
    None = 0,
    MissingFile = 1,
    InvalidType = 2,
    FileTooLarge = 3,
    InvalidJpeg = 4,
    InvalidDimensions = 5,
    SaveFailed = 6
}

public sealed record NewsBannerImageSaveResult(
    bool Ok,
    string? PublicImagePath,
    NewsBannerImageError Error,
    int ActualWidth = 0,
    int ActualHeight = 0);

