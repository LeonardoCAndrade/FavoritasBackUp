using System.Net;
using System.Text;
using FavoritasBackUp.Services;

static class ConnectionChecks
{
    public static async Task RunAsync()
    {
        static void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
        var config = new CloudConfigurationService(new Storage());
        using (var client = new HttpClient(new Handler(_ => throw new Exception("Missing configuration must not send"))))
            Check((await new NextcloudConnectionService(client, config).TestAsync()).Contains("Revise a configuração"), "Missing configuration");
        foreach (var prefix in new[] { "", "/nextcloud" })
        {
            await config.SaveAsync(new() { Url = $"https://cloud.example:8443{prefix}/remote.php/dav/files/user/", Username = "user", Password = "app-password" });
            var calls = 0;
            using var client = new HttpClient(new Handler(request =>
            {
                calls++;
                Check(request.Method == HttpMethod.Get && request.Content is null, "Connection test must only GET");
                Check(request.RequestUri!.AbsoluteUri == $"https://cloud.example:8443{prefix}/ocs/v2.php/cloud/user?format=json", "OCS endpoint/prefix/port");
                Check(request.Headers.Authorization?.Scheme == "Basic" &&
                    request.Headers.Authorization.Parameter == Convert.ToBase64String(Encoding.UTF8.GetBytes("user:app-password")), "OCS Basic");
                Check(request.Headers.GetValues("OCS-APIRequest").Single() == "true", "OCS header");
                Check(request.Headers.Accept.ToString() == "application/json", "OCS Accept");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent("{\"ocs\":{\"meta\":{\"status\":\"ok\",\"statuscode\":200},\"data\":{\"id\":\"canonical-user\"}}}") });
            }));
            Check((await new NextcloudConnectionService(client, config).TestAsync()).StartsWith("Conexão e autenticação"), "OCS success");
            Check(calls == 1, "Exactly one GET expected");
        }
        foreach (var status in new[] { 401, 403, 404, 302, 500 })
        {
            using var client = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status))));
            Check((await new NextcloudConnectionService(client, config).TestAsync()).Contains($"HTTP {status}"), "HTTP failure");
        }
        foreach (var body in new[] { "{}", "null", "[]", "not json", "{\"ocs\":{\"meta\":{\"status\":\"failure\",\"statuscode\":997}}}",
            "{\"ocs\":{\"meta\":{\"status\":\"ok\",\"statuscode\":100}}}",
            "{\"ocs\":{\"meta\":{\"status\":\"failure\",\"statuscode\":200}}}",
            "{\"ocs\":{\"meta\":{\"status\":\"ok\",\"statuscode\":200}}}",
            "{\"ocs\":{\"meta\":{\"status\":\"ok\",\"statuscode\":200},\"data\":{\"id\":\"\"}}}" })
        {
            using var client = new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) })));
            Check(!(await new NextcloudConnectionService(client, config).TestAsync()).StartsWith("Conexão e autenticação"), "Malformed/failed OCS must not succeed");
        }
        var log = new List<string>();
        using (var client = new HttpClient(new Handler(_ => throw new HttpRequestException("outer app-password",
            new System.Security.Authentication.AuthenticationException("inner user " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:app-password")))))))
        {
            var result = await new NextcloudConnectionService(client, config) { WriteDiagnostic = log.Add }.TestAsync();
            Check(result.Contains("certificado HTTPS") && log.Any(line => line.Contains("AuthenticationException")), "TLS message and inner exception diagnosis");
            Check(!string.Join("", log).Contains("app-password") && !result.Contains("app-password") &&
                !string.Join("", log).Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("user:app-password"))), "Credentials in diagnostics");
        }
        foreach (var url in new[] { "http://cloud.example/remote.php/dav/files/user/", "https://user:secret@cloud.example/", "https://cloud.example/" })
        {
            await config.SaveAsync(new() { Url = url, Username = "user", Password = "app-password" });
            using var client = new HttpClient(new Handler(_ => throw new Exception("Invalid configuration must not send")));
            Check((await new NextcloudConnectionService(client, config).TestAsync()).Contains("URL WebDAV"), "Invalid URL validation");
        }
        await config.SaveAsync(new() { Url = "https://cloud.example/remote.php/dav/files/user/", Username = "user", Password = "app-password" });
        foreach (var failure in new[]
        {
            new HttpRequestException("offline"),
            new HttpRequestException(HttpRequestError.SecureConnectionError, "certificate")
        })
        {
            using var client = new HttpClient(new Handler(_ => throw failure));
            var service = new NextcloudConnectionService(client, config);
            var expected = failure.HttpRequestError == HttpRequestError.SecureConnectionError ? "certificado HTTPS" : "servidor está disponível";
            Check((await service.TestAsync()).Contains(expected), "Distinct connection/certificate error");
            Check((await service.TestAsync()).Contains(expected), "Retry after failure");
        }
        using (var client = new HttpClient(new TimeoutHandler()) { Timeout = TimeSpan.FromMilliseconds(50) })
            Check((await new NextcloudConnectionService(client, config).TestAsync()).Contains("tempo limite"), "HTTP timeout");
        using (var client = new HttpClient(new Handler(_ => throw new OperationCanceledException())))
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            try { await new NextcloudConnectionService(client, config).TestAsync(cancellation.Token); throw new Exception("Cancellation ignored"); }
            catch (OperationCanceledException) { }
        }
        Console.WriteLine("PASS OCS GET only, authenticated user, endpoint/prefix, Basic/headers, HTTP/OCS failures, invalid JSON/URL, missing configuration, connection/TLS errors, timeout, retry, redaction and cancellation");
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Expected cancellation");
        }
    }
}
