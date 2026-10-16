using FavoritasBackUp.Models;

namespace FavoritasBackUp.Services;

public interface INextcloudBackupService
{
    Task<BackupResult> BackupTodayAsync(CancellationToken cancellationToken = default);
}
