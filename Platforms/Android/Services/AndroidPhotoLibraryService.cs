using Android.Content;
using Android.OS;
using Android.Provider;
using FavoritasBackUp.Models;
using FavoritasBackUp.Services;

namespace FavoritasBackUp.Platforms.Android.Services;

public sealed class AndroidPhotoLibraryService : IPhotoLibraryService
{
    public Task<IReadOnlyList<PhotoInfo>> GetFavoritePhotosAsync(CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<PhotoInfo>>(() => ReadFavorites(cancellationToken), cancellationToken);

    public Task<int> GetTotalPhotosCountAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Query(cancellationToken, false, (cursor, _) => cursor.Count), cancellationToken);

    private static IReadOnlyList<PhotoInfo> ReadFavorites(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
            throw new NotSupportedException("O indicador público de favoritas requer Android 11 ou superior.");

        return Query<IReadOnlyList<PhotoInfo>>(cancellationToken, true, (cursor, collection) =>
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(30))
                throw new NotSupportedException("O indicador público de favoritas requer Android 11 ou superior.");
            var idColumn = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Id);
            var nameColumn = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.DisplayName);
            var favoriteColumn = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.IsFavorite);
            var dateColumn = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.DateAdded);
            var photos = new List<PhotoInfo>();
            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = cursor.GetLong(idColumn);
                DateTime? date = null;
                if (!cursor.IsNull(dateColumn))
                {
                    try { date = DateTimeOffset.FromUnixTimeSeconds(cursor.GetLong(dateColumn)).UtcDateTime; }
                    catch (ArgumentOutOfRangeException) { /* Invalid provider metadata: keep the date unknown. */ }
                }
                using var uri = ContentUris.WithAppendedId(collection, id);
                photos.Add(new PhotoInfo
                {
                    Id = id,
                    FileName = cursor.IsNull(nameColumn) ? string.Empty : cursor.GetString(nameColumn) ?? string.Empty,
                    ContentUri = uri?.ToString() ?? throw new InvalidOperationException("Não foi possível gerar a URI da imagem."),
                    IsFavorite = cursor.GetInt(favoriteColumn) == 1,
                    DateAddedUtc = date
                });
            }
            return photos.AsReadOnly();
        });
    }

    private static T Query<T>(CancellationToken token, bool favorites,
        Func<global::Android.Database.ICursor, global::Android.Net.Uri, T> read)
    {
        token.ThrowIfCancellationRequested();
        using var signal = new CancellationSignal();
        using var registration = token.Register(signal.Cancel);
        // The external collection includes shared images across external storage volumes.
        var collection = MediaStore.Images.Media.ExternalContentUri
            ?? throw new NotSupportedException("A biblioteca de imagens não está disponível neste dispositivo.");
        if (favorites && !OperatingSystem.IsAndroidVersionAtLeast(30))
            throw new NotSupportedException("O indicador público de favoritas requer Android 11 ou superior.");
        var projection = favorites && OperatingSystem.IsAndroidVersionAtLeast(30)
            ? new[] { MediaStore.Images.Media.InterfaceConsts.Id, MediaStore.Images.Media.InterfaceConsts.DisplayName,
                MediaStore.Images.Media.InterfaceConsts.IsFavorite, MediaStore.Images.Media.InterfaceConsts.DateAdded }
            : new[] { MediaStore.Images.Media.InterfaceConsts.Id };
        try
        {
            var resolver = global::Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Não foi possível acessar a biblioteca de imagens.");
            using var cursor = resolver.Query(collection, projection,
                favorites ? $"{projection[2]} = ?" : null,
                favorites ? new[] { "1" } : null, null, signal)
                ?? throw new InvalidOperationException("O MediaStore não retornou uma consulta válida.");
            var result = read(cursor, collection);
            token.ThrowIfCancellationRequested();
            return result;
        }
        catch (global::Android.OS.OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw new System.OperationCanceledException(token);
        }
        catch (Java.Lang.SecurityException exception)
        {
            throw new UnauthorizedAccessException("O acesso às imagens foi negado ou revogado.", exception);
        }
        catch (Java.Lang.IllegalArgumentException exception) when (favorites)
        {
            throw new NotSupportedException("O MediaStore deste dispositivo não disponibilizou o indicador público IS_FAVORITE.", exception);
        }
        catch (global::Android.Database.Sqlite.SQLiteException exception) when (favorites)
        {
            throw new NotSupportedException("Não foi possível consultar o indicador público IS_FAVORITE neste dispositivo.", exception);
        }
    }
}
