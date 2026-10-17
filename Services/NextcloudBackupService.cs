using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FavoritasBackUp.Models;

namespace FavoritasBackUp.Services;

public sealed class NextcloudBackupService(
    HttpClient client, CloudConfigurationService configurationService,
    IPhotoLibraryService photoLibrary) : INextcloudBackupService
{
    private readonly SemaphoreSlim _operation = new(1, 1);

    public async Task<BackupResult> BackupTodayAsync(CancellationToken cancellationToken = default)
    {
        if (!await _operation.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Já existe um backup em andamento.");
        try
        {
            var today = DateTime.Today;
            var zone = TimeZoneInfo.Local;
            var favorites = await photoLibrary.GetFavoritePhotosAsync(cancellationToken);
            var photos = favorites.Where(p => p.IsFavorite && p.DateTakenUtc is DateTime taken &&
                TimeZoneInfo.ConvertTimeFromUtc(taken, zone).Date == today).ToList();
            if (photos.Count == 0) return new(0, 0, 0, 0);

            Uri folder;
            AuthenticationHeaderValue authorization;
            try
            {
                var config = await configurationService.GetAsync();
                if (config is null || !Uri.TryCreate(config.Url, UriKind.Absolute, out var root) ||
                    root.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(root.UserInfo) ||
                    !string.IsNullOrEmpty(root.Query) || !string.IsNullOrEmpty(root.Fragment) ||
                    string.IsNullOrWhiteSpace(config.Username) || config.Username.Contains(':'))
                    return new(photos.Count, 0, 0, photos.Count,
                        "Revise a configuração: informe a URL WebDAV HTTPS, o usuário e a senha de aplicativo do Nextcloud.");
                folder = new Uri(new Uri(root.AbsoluteUri.TrimEnd('/') + "/"), "FavoritasBackUp/");
                authorization = new("Basic", Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}")));
                await EnsureFolderAsync(folder, authorization, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                return new(photos.Count, 0, 0, photos.Count, DescribeError(exception));
            }

            int uploaded = 0, skipped = 0, failed = 0;
            string? error = null;
            foreach (var photo in photos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // Two streaming reads avoid buffering the image and give identical content a stable name.
                    byte[] hash;
                    using (var source = await photoLibrary.OpenReadAsync(photo, cancellationToken))
                        hash = await SHA256.HashDataAsync(source, cancellationToken);
                    var name = Convert.ToHexString(hash).ToLowerInvariant();
                    using var sourceContent = await photoLibrary.OpenReadAsync(photo, cancellationToken);
                    using var request = CreateRequest(HttpMethod.Put, new Uri(folder, name), authorization);
                    request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Any);
                    request.Content = new StreamContent(sourceContent);
                    request.Content.Headers.ContentType = new("application/octet-stream");
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) skipped++;
                    else
                    {
                        RequireSuccess(response);
                        uploaded++;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    failed++;
                    error ??= DescribeError(exception);
                }
            }
            return new(photos.Count, uploaded, skipped, failed, error);
        }
        finally { _operation.Release(); }
    }

    private async Task EnsureFolderAsync(Uri folder, AuthenticationHeaderValue authorization, CancellationToken token)
    {
        using var request = CreateRequest(new HttpMethod("MKCOL"), folder, authorization);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode != HttpStatusCode.MethodNotAllowed)
        {
            RequireSuccess(response);
            return;
        }
        // 405 can also mean an unsupported method or an existing file. Verify the collection.
        using var probe = CreateRequest(new HttpMethod("PROPFIND"), folder, authorization);
        probe.Headers.Add("Depth", "0");
        probe.Content = new StringContent("<d:propfind xmlns:d=\"DAV:\"><d:prop><d:resourcetype/></d:prop></d:propfind>", Encoding.UTF8, "application/xml");
        using var existing = await client.SendAsync(probe, HttpCompletionOption.ResponseHeadersRead, token);
        RequireSuccess(existing);
        using var stream = await existing.Content.ReadAsStreamAsync(token);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
            MaxCharactersInDocument = 1024 * 1024
        });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, token);
        XNamespace dav = "DAV:";
        if (!document.Descendants(dav + "propstat").Any(p =>
            p.Element(dav + "status")?.Value.Contains(" 200 ", StringComparison.Ordinal) == true &&
            p.Element(dav + "prop")?.Element(dav + "resourcetype")?.Element(dav + "collection") is not null))
            throw new InvalidOperationException("A pasta de backup não está disponível no servidor.");
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, AuthenticationHeaderValue authorization)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = authorization;
        return request;
    }

    private static void RequireSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("O servidor recusou a operação.", null, response.StatusCode);
    }

    private static bool IsTlsError(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is System.Security.Authentication.AuthenticationException ||
                cause is HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError })
                return true;
#if ANDROID
            if (cause is Javax.Net.Ssl.SSLException or Java.Security.Cert.CertificateException)
                return true;
#endif
        }
        return false;
    }

    private static string DescribeError(Exception exception) => exception switch
    {
        _ when IsTlsError(exception) =>
            "Falha de segurança TLS/HTTPS. Verifique a confiança na CA do Caddy, a validade do certificado e se ele corresponde ao endereço configurado.",
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } =>
            "HTTP 401: credenciais inválidas. Verifique o usuário e a senha de aplicativo do Nextcloud.",
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden } =>
            "HTTP 403: acesso negado. Verifique a permissão de escrita no Nextcloud.",
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } =>
            "HTTP 404: destino WebDAV não encontrado. Revise a URL completa, incluindo o usuário no caminho.",
        HttpRequestException { StatusCode: not null } http =>
            $"O servidor retornou HTTP {(int)http.StatusCode.Value}. Revise a URL WebDAV e a disponibilidade do Nextcloud. Redirecionamentos não são seguidos.",
        HttpRequestException or OperationCanceledException => "Falha de conexão ou tempo limite. Verifique a rede e a disponibilidade do servidor e tente novamente.",
        UnauthorizedAccessException => "O acesso às imagens foi negado ou revogado. Revise as permissões do Android.",
        IOException => "Não foi possível ler ou transferir uma imagem. Tente novamente.",
        _ => "Não foi possível preparar ou concluir o backup. Revise a configuração e tente novamente."
    };
}
