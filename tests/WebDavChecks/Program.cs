using System.Net;
using System.Security.Cryptography;
using System.Text;
using FavoritasBackUp.Models;
using FavoritasBackUp.Services;
using Microsoft.Maui.Storage;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static PhotoInfo Photo(int id, DateTime? date, bool favorite = true) => new()
{
    Id = id, ContentUri = $"content://images/{id}", FileName = "../../unsafe.jpg",
    DateTakenUtc = date, DateAddedUtc = DateTime.UtcNow, IsFavorite = favorite
};

var today = DateTime.Today.AddHours(12).ToUniversalTime();
var library = new PhotoLibrary
{
    Photos = [Photo(1, DateTime.Today.ToUniversalTime()), Photo(2, today),
        Photo(3, DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime()),
        Photo(4, DateTime.Today.AddTicks(-1).ToUniversalTime()), Photo(5, null),
        Photo(6, DateTime.Today.AddDays(1).ToUniversalTime()), Photo(7, today, false)]
};
var config = new CloudConfigurationService(new Storage());
await config.SaveAsync(new() { Url = "https://cloud.example/remote.php/dav/files/user", Username = "user", Password = "app-password" });
var putCount = 0;
var handler = new Handler(async request =>
{
    Check(request.RequestUri!.Scheme == "https", "HTTPS required");
    Check(request.Headers.Authorization?.Parameter == Convert.ToBase64String(Encoding.UTF8.GetBytes("user:app-password")), "Basic auth");
    if (request.Method.Method == "MKCOL") return new(HttpStatusCode.Created);
    Check(request.Method == HttpMethod.Put, "Expected PUT");
    Check(request.Headers.IfNoneMatch.ToString() == "*", "Missing no-overwrite condition");
    var data = await request.Content!.ReadAsByteArrayAsync();
    var hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    Check(request.RequestUri.AbsolutePath.EndsWith("/FavoritasBackUp/" + hash), "Unsafe or unstable filename");
    return new(++putCount switch { 1 => HttpStatusCode.Created, 2 => HttpStatusCode.PreconditionFailed, _ => HttpStatusCode.InternalServerError });
});
var service = new NextcloudBackupService(new HttpClient(handler), config, library);
var result = await service.BackupTodayAsync();
Check(result.Total == 3 && result.Uploaded == 1 && result.Skipped == 1 && result.Failed == 1, "Date filter or accounting failed");
Check(library.Opened == 6 && library.Disposed == 6, "Streams must be disposed");
Console.WriteLine("PASS local capture date, unknown date exclusion, counts, Basic, PUT condition, safe stable names, stream disposal");

foreach (var status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Conflict })
{
    var calls = 0;
    var failure = new NextcloudBackupService(new HttpClient(new Handler(_ =>
    {
        calls++;
        return Task.FromResult(new HttpResponseMessage(status));
    })), config, library);
    var failed = await failure.BackupTodayAsync();
    Check(failed.Failed == 3 && failed.Uploaded == 0 && calls == 1, "Folder error must prevent PUT");
}
Console.WriteLine("PASS authentication, HTTP errors and redirect response accounting");

foreach (var isCollection in new[] { true, false })
{
    var existing = new NextcloudBackupService(new HttpClient(new Handler(request =>
    {
        var response = request.Method.Method switch
        {
            "MKCOL" => new HttpResponseMessage(HttpStatusCode.MethodNotAllowed),
            "PROPFIND" => new HttpResponseMessage((HttpStatusCode)207)
            {
                Content = new StringContent("<d:multistatus xmlns:d='DAV:'><d:response><d:propstat><d:prop><d:resourcetype>" +
                    (isCollection ? "<d:collection/>" : "") +
                    "</d:resourcetype></d:prop><d:status>HTTP/1.1 200 OK</d:status></d:propstat></d:response></d:multistatus>")
            },
            _ => new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
        };
        return Task.FromResult(response);
    })), config, library);
    var outcome = await existing.BackupTodayAsync();
    Check(isCollection ? outcome.Skipped == 3 && outcome.Failed == 0 : outcome.Failed == 3, "405 must verify collection");
}
Console.WriteLine("PASS existing folder and file occupying folder path");

await config.SaveAsync(new() { Url = "http://cloud.example/", Username = "user", Password = "app-password" });
var insecure = await service.BackupTodayAsync();
Check(insecure.Failed == 3 && putCount == 3, "HTTP must be rejected before sending credentials");
Console.WriteLine("PASS insecure URL refusal");

await config.SaveAsync(new() { Url = "https://cloud.example/", Username = "user", Password = "app-password" });
var offline = new NextcloudBackupService(new HttpClient(new Handler(_ => throw new HttpRequestException("offline"))), config, library);
Check((await offline.BackupTodayAsync()).Failed == 3, "Connection failure accounting");
var entered = new TaskCompletionSource();
var release = new TaskCompletionSource();
var concurrent = new NextcloudBackupService(new HttpClient(new Handler(async _ =>
{
    entered.TrySetResult();
    await release.Task;
    return new(HttpStatusCode.Unauthorized);
})), config, library);
var first = concurrent.BackupTodayAsync();
await entered.Task;
try { await concurrent.BackupTodayAsync(); throw new Exception("Concurrent backup was allowed"); }
catch (InvalidOperationException) { }
release.SetResult();
await first;
using var canceled = new CancellationTokenSource();
canceled.Cancel();
try { await service.BackupTodayAsync(canceled.Token); throw new Exception("Cancellation ignored"); }
catch (OperationCanceledException) { }
Console.WriteLine("PASS connection failure, concurrency guard and cancellation");

var denied = new PhotoLibrary { Photos = [Photo(1, today)], DenyRead = true };
var unreadable = new NextcloudBackupService(new HttpClient(new Handler(_ =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)))), config, denied);
Check((await unreadable.BackupTodayAsync()).Failed == 1, "Revoked read permission must count as failure");
var empty = new NextcloudBackupService(new HttpClient(new Handler(_ =>
    throw new Exception("Empty backup must not contact server"))), config, new PhotoLibrary());
Check(await empty.BackupTodayAsync() == new BackupResult(0, 0, 0, 0), "Empty result");
Console.WriteLine("PASS revoked read permission and empty backup");

sealed class Storage : ISecureStorage
{
    private readonly Dictionary<string, string> values = [];
    public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key));
    public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; }
}

sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
}

sealed class PhotoLibrary : IPhotoLibraryService
{
    public IReadOnlyList<PhotoInfo> Photos { get; init; } = [];
    public int Opened, Disposed;
    public bool DenyRead { get; init; }
    public Task<IReadOnlyList<PhotoInfo>> GetFavoritePhotosAsync(CancellationToken cancellationToken = default) => Task.FromResult(Photos);
    public Task<int> GetTotalPhotosCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Photos.Count);
    public Task<Stream> OpenReadAsync(PhotoInfo photo, CancellationToken cancellationToken = default)
    {
        if (DenyRead) throw new UnauthorizedAccessException();
        Opened++;
        return Task.FromResult<Stream>(new TrackedStream(Encoding.UTF8.GetBytes(photo.ContentUri), () => Disposed++));
    }
}

sealed class TrackedStream(byte[] data, Action disposed) : MemoryStream(data)
{
    private bool closed;
    protected override void Dispose(bool disposing)
    {
        if (!closed) { closed = true; disposed(); }
        base.Dispose(disposing);
    }
}
