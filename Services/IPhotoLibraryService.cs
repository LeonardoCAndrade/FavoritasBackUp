using FavoritasBackUp.Models;

namespace FavoritasBackUp.Services;

public interface IPhotoLibraryService
{
    Task<IReadOnlyList<PhotoInfo>> GetFavoritePhotosAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalPhotosCountAsync(CancellationToken cancellationToken = default);
}
