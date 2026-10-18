using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FavoritasBackUp.Models;

namespace FavoritasBackUp.Services;

public sealed class NextcloudConnectionService(
    HttpClient client, CloudConfigurationService configurationService)
{
    // Injectable diagnostic destination keeps tests independent of device logging.
    public Action<string> WriteDiagnostic { get; init; } = message => System.Diagnostics.Debug.WriteLine(message);

    public async Task<string> TestAsync(CancellationToken cancellationToken = default)
    {
        CloudConfiguration? config = null;
        string? basic = null;
        try
        {
            config = await configurationService.GetAsync();
            if (config is null || !Uri.TryCreate(config.Url, UriKind.Absolute, out var root) ||
                root.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(root.UserInfo) ||
                !string.IsNullOrEmpty(root.Query) || !string.IsNullOrEmpty(root.Fragment) ||
                string.IsNullOrWhiteSpace(config.Username) || config.Username.Contains(':'))
                return "Revise a configuração: informe a URL WebDAV HTTPS, o usuário e a senha de aplicativo.";

            // Preserve installation prefixes, e.g. /nextcloud/remote.php/dav/files/user/.
            var marker = root.AbsolutePath.IndexOf("/remote.php/dav/files/", StringComparison.Ordinal);
            if (marker < 0)
                return "Informe a URL WebDAV completa, terminando em /remote.php/dav/files/USUARIO/.";
            var endpoint = new Uri(root.GetLeftPart(UriPartial.Authority) +
                root.AbsolutePath[..marker] + "/ocs/v2.php/cloud/user?format=json");
            basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}"));
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Headers.Add("OCS-APIRequest", "true");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using var response = await client.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var hint = (int)response.StatusCode switch
                {
                    401 => "Verifique o usuário e a senha de aplicativo.",
                    403 => "O acesso foi negado pelo servidor.",
                    404 => "Endpoint OCS não encontrado; verifique a URL configurada.",
                    >= 300 and < 400 => "Redirecionamento recusado; configure a URL HTTPS final.",
                    _ => "Verifique o servidor Nextcloud e o proxy Caddy."
                };
                var message = $"Falha na conexão: HTTP {(int)response.StatusCode}. {hint}";
                WriteDiagnostic(message);
                return message;
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (json.RootElement.ValueKind != JsonValueKind.Object ||
                !json.RootElement.TryGetProperty("ocs", out var ocs) || ocs.ValueKind != JsonValueKind.Object ||
                !ocs.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object ||
                !meta.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String ||
                !meta.TryGetProperty("statuscode", out var code) || code.ValueKind != JsonValueKind.Number ||
                !code.TryGetInt32(out var number))
                return "Resposta inválida: o servidor não retornou ocs.meta com status e statuscode.";
            // OCS v2 uses HTTP-style status codes (200 for success).
            if (status.GetString() != "ok" || number != 200)
            {
                var message = $"Falha na autenticação OCS: statuscode {number}. Verifique as credenciais e o servidor.";
                WriteDiagnostic(message);
                return message;
            }
            return "Conexão e autenticação com o Nextcloud validadas (HTTP e OCS 200).";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var details = new List<string>();
            for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            {
                // Before configuration is loaded, omit arbitrary storage exception text.
                var detail = $"{cause.GetType().FullName} (0x{cause.HResult:X8})";
                if (config is not null) detail += ": " + cause.Message;
                detail = Redact(detail, config, basic);
                details.Add(detail);
                WriteDiagnostic(detail + "\n" + Redact(cause.StackTrace ?? "", config, basic));
            }
            return "Falha no teste de conexão. Verifique HTTPS/CA do Caddy, rede e credenciais.\n" +
                string.Join("\nCausa: ", details);
        }
    }

    private static string Redact(string text, CloudConfiguration? config, string? basic)
    {
        var secrets = new[] { config?.Password, config?.Username, config?.Url, basic }
            .Where(value => !string.IsNullOrEmpty(value)).Select(value => value!)
            .SelectMany(value => new[] { value, Uri.EscapeDataString(value), JsonEncodedText.Encode(value).ToString() })
            .OrderByDescending(value => value.Length);
        foreach (var secret in secrets) text = text.Replace(secret, "[oculto]", StringComparison.Ordinal);
        return text;
    }
}
