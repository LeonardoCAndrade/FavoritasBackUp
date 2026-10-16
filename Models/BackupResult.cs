namespace FavoritasBackUp.Models;

public sealed record BackupResult(int Total, int Uploaded, int Skipped, int Failed, string? Error = null);
