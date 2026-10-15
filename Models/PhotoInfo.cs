namespace FavoritasBackUp.Models;

public sealed class PhotoInfo
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentUri { get; init; } = string.Empty;
    public bool IsFavorite { get; init; }
    public DateTime? DateAddedUtc { get; init; }
}
