namespace MiniApp.Features.Winners;

public sealed record WinnerEntryDto(long Id, string Name, string WinningAmount, string? Quote, string PhotoUrl);

public sealed record WinnersListResult(bool Ok, IReadOnlyList<WinnerEntryDto> Winners);

public enum WinnerImageError
{
    None = 0,
    MissingFile = 1,
    InvalidType = 2,
    FileTooLarge = 3,
    SaveFailed = 4
}

public sealed record WinnerImageSaveResult(
    bool Ok,
    string? PublicImagePath,
    WinnerImageError Error);


