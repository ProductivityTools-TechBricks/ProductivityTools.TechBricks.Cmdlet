using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProductivityTools.TechBricks.Data.Auth
{
    /// <summary>
    /// Signs the user in through the browser.
    ///
    /// A temporary HTTP server is started on http://localhost:{random port}/
    /// (localhost is an authorized Firebase domain by default). It serves a page
    /// which signs the user in with Google using the Firebase JS SDK - exactly
    /// like the web application - and posts the Firebase refresh token back to
    /// the server. A random state value protects the callback from other
    /// local pages.
    /// </summary>
    public class BrowserLogin
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        private readonly Action<string> log;

        public BrowserLogin(Action<string> log)
        {
            this.log = log;
        }

        public async Task<LoginResult> LoginAsync(CancellationToken cancellationToken)
        {
            int port = GetFreePort();
            string state = CreateState();
            string baseUrl = $"http://localhost:{port}/";

            using var listener = new HttpListener();
            listener.Prefixes.Add(baseUrl);
            listener.Start();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(DefaultTimeout);
            using var registration = timeout.Token.Register(() => listener.Stop());

            log($"Opening browser for sign in: {baseUrl}");
            log("If the browser did not open, open the address above manually.");
            OpenBrowser(baseUrl);

            try
            {
                while (true)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    LoginResult? result = await HandleRequestAsync(context, state);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Sign in was cancelled.", ex, cancellationToken);
                }
                throw new TimeoutException($"Sign in was not completed within {DefaultTimeout.TotalMinutes} minutes.", ex);
            }
        }

        private static async Task<LoginResult?> HandleRequestAsync(HttpListenerContext context, string state)
        {
            var request = context.Request;
            var response = context.Response;
            try
            {
                string path = request.Url?.AbsolutePath ?? "/";
                if (request.HttpMethod == "GET" && path == "/")
                {
                    await WriteAsync(response, HttpStatusCode.OK, "text/html; charset=utf-8", GetLoginPage(state));
                    return null;
                }

                if (request.HttpMethod == "POST" && path == "/callback")
                {
                    // Only the page served by this listener may call back.
                    string? origin = request.Headers["Origin"];
                    string expectedOrigin = $"http://localhost:{request.Url!.Port}";
                    if (origin != null && !string.Equals(origin, expectedOrigin, StringComparison.OrdinalIgnoreCase))
                    {
                        await WriteAsync(response, HttpStatusCode.Forbidden, "text/plain", "Invalid origin");
                        return null;
                    }

                    using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                    string body = await reader.ReadToEndAsync();
                    var callback = JsonSerializer.Deserialize<CallbackPayload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (callback == null
                        || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(callback.State ?? string.Empty), Encoding.UTF8.GetBytes(state))
                        || string.IsNullOrEmpty(callback.RefreshToken))
                    {
                        await WriteAsync(response, HttpStatusCode.BadRequest, "text/plain", "Invalid sign in response");
                        return null;
                    }

                    await WriteAsync(response, HttpStatusCode.OK, "text/plain", "OK");
                    return new LoginResult(callback.RefreshToken, callback.Email);
                }

                await WriteAsync(response, HttpStatusCode.NotFound, "text/plain", "Not found");
                return null;
            }
            catch (JsonException)
            {
                await WriteAsync(response, HttpStatusCode.BadRequest, "text/plain", "Invalid JSON");
                return null;
            }
        }

        private static async Task WriteAsync(HttpListenerResponse response, HttpStatusCode status, string contentType, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.StatusCode = (int)status;
            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;
            response.Headers["Cache-Control"] = "no-store";
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();
        }

        private static string GetLoginPage(string state)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("ProductivityTools.TechBricks.Data.LoginPage.html")
                ?? throw new InvalidOperationException("Login page resource is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd()
                .Replace("__API_KEY__", TechBricksConfig.FirebaseApiKey)
                .Replace("__AUTH_DOMAIN__", TechBricksConfig.FirebaseAuthDomain)
                .Replace("__PROJECT_ID__", TechBricksConfig.FirebaseProjectId)
                .Replace("__APP_ID__", TechBricksConfig.FirebaseAppId)
                .Replace("__STATE__", state);
        }

        private static string CreateState()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        private static int GetFreePort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        private void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                log($"Unable to open the browser automatically: {ex.Message}");
            }
        }

        private class CallbackPayload
        {
            public string? State { get; set; }
            public string? RefreshToken { get; set; }
            public string? Email { get; set; }
        }
    }

    public record LoginResult(string RefreshToken, string? Email);
}
