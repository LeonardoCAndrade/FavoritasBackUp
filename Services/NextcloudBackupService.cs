using System.Net;
using System.Net.Http.Headers;
using System.Text;
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
                folder = new Uri(root.AbsoluteUri.TrimEnd('/') + "/");
                authorization = new("Basic", Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}")));
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
                    var name = photo.FileName;
                    if (string.IsNullOrWhiteSpace(name) || name is "." or ".." ||
                        name.Contains('/') || name.Contains('\\'))
                        throw new IOException("O nome original da imagem está ausente ou é inválido.");
                    using var sourceContent = await photoLibrary.OpenReadAsync(photo, cancellationToken);
                    using var request = CreateRequest(HttpMethod.Put, new Uri(folder, Uri.EscapeDataString(name)), authorization);
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
            "HTTP 404: diretório de destino não encontrado. Crie a pasta manualmente no Nextcloud e confira a URL WebDAV configurada.",
        HttpRequestException { StatusCode: HttpStatusCode.Conflict } =>
            "HTTP 409: caminho de destino indisponível. Crie a pasta manualmente no Nextcloud e confira a URL WebDAV configurada.",
        HttpRequestException { StatusCode: not null } http =>
            $"O servidor retornou HTTP {(int)http.StatusCode.Value}. Revise a URL WebDAV e a disponibilidade do Nextcloud. Redirecionamentos não são seguidos.",
        HttpRequestException or OperationCanceledException => "Falha de conexão ou tempo limite. Verifique a rede e a disponibilidade do servidor e tente novamente.",
        UnauthorizedAccessException => "O acesso às imagens foi negado ou revogado. Revise as permissões do Android.",
        IOException => "Não foi possível ler ou transferir uma imagem. Tente novamente.",
        _ => "Não foi possível preparar ou concluir o backup. Revise a configuração e tente novamente."
    };
}
