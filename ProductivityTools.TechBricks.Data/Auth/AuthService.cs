using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductivityTools.TechBricks.Data.Auth
{
    /// <summary>
    /// Provides short-lived (1h) Firebase ID tokens minted from the stored
    /// long-lived refresh token - the same mechanism the Firebase JS SDK uses
    /// in the web application.
    /// </summary>
    public class AuthService
    {
        private const string SecureTokenUrl = "https://securetoken.googleapis.com/v1/token?key=" + TechBricksConfig.FirebaseApiKey;

        // Refresh a bit earlier than required, to survive clock skew and slow calls.
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

        private static readonly HttpClient HttpClient = new();

        private readonly TokenStore tokenStore;
        private readonly Action<string> log;

        // ID token cache shared by all cmdlet invocations in the PowerShell session.
        private static string? cachedIdToken;
        private static DateTimeOffset cachedIdTokenExpiry;
        private static readonly SemaphoreSlim Lock = new(1, 1);

        public AuthService(Action<string> log) : this(new TokenStore(), log)
        {
        }

        public AuthService(TokenStore tokenStore, Action<string> log)
        {
            this.tokenStore = tokenStore;
            this.log = log;
        }

        public string? SignedInEmail => tokenStore.Load()?.Email;

        /// <summary>
        /// Signs in through the browser and stores the session.
        /// </summary>
        public async Task<string?> LoginAsync(CancellationToken cancellationToken)
        {
            var result = await new BrowserLogin(log).LoginAsync(cancellationToken);
            await Lock.WaitAsync(cancellationToken);
            try
            {
                ClearCache();
                // Validate the session before storing it.
                var (_, refreshToken) = await RefreshAsync(result.RefreshToken, cancellationToken);
                tokenStore.Save(new StoredSession { RefreshToken = refreshToken, Email = result.Email });
            }
            finally
            {
                Lock.Release();
            }
            return result.Email;
        }

        public bool Logout()
        {
            ClearCache();
            return tokenStore.Delete();
        }

        /// <summary>
        /// Returns a valid ID token. When the user has never signed in (or the
        /// session was revoked) the browser sign in is started.
        /// </summary>
        public async Task<string> GetIdTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            await Lock.WaitAsync(cancellationToken);
            try
            {
                if (!forceRefresh && cachedIdToken != null && DateTimeOffset.UtcNow < cachedIdTokenExpiry - ExpiryMargin)
                {
                    return cachedIdToken;
                }

                var session = tokenStore.Load();
                if (session != null)
                {
                    try
                    {
                        var (idToken, refreshToken) = await RefreshAsync(session.RefreshToken, cancellationToken);
                        if (refreshToken != session.RefreshToken)
                        {
                            tokenStore.Save(new StoredSession { RefreshToken = refreshToken, Email = session.Email });
                        }
                        return idToken;
                    }
                    catch (SessionExpiredException ex)
                    {
                        log($"Stored session is no longer valid ({ex.Message}). Signing in again.");
                        tokenStore.Delete();
                        ClearCache();
                    }
                }
            }
            finally
            {
                Lock.Release();
            }

            log("You are not signed in to TechBricks.");
            await LoginAsync(cancellationToken);
            return cachedIdToken ?? throw new InvalidOperationException("Sign in did not produce an ID token.");
        }

        /// <summary>
        /// Exchanges the refresh token for a new ID token. Returns the ID token
        /// and the refresh token to keep (Firebase may rotate it).
        /// </summary>
        private async Task<(string IdToken, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            });

            using var response = await HttpClient.PostAsync(SecureTokenUrl, content, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // e.g. TOKEN_EXPIRED, USER_DISABLED, USER_NOT_FOUND, INVALID_REFRESH_TOKEN
                throw new SessionExpiredException(ExtractError(body));
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Unable to refresh the TechBricks session: {(int)response.StatusCode} {ExtractError(body)}");
            }

            var token = JsonSerializer.Deserialize<SecureTokenResponse>(body)
                ?? throw new HttpRequestException("Empty response from securetoken.googleapis.com");

            int expiresIn = int.TryParse(token.ExpiresIn, out var seconds) ? seconds : 3600;
            cachedIdToken = token.IdToken;
            cachedIdTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return (token.IdToken, string.IsNullOrEmpty(token.RefreshToken) ? refreshToken : token.RefreshToken);
        }

        private static void ClearCache()
        {
            cachedIdToken = null;
            cachedIdTokenExpiry = DateTimeOffset.MinValue;
        }

        private static string ExtractError(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? body;
                }
            }
            catch (JsonException)
            {
            }
            return body;
        }

        private class SecureTokenResponse
        {
            [JsonPropertyName("id_token")]
            public string IdToken { get; set; } = string.Empty;

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("expires_in")]
            public string? ExpiresIn { get; set; }
        }
    }

    public class SessionExpiredException : Exception
    {
        public SessionExpiredException(string message) : base(message)
        {
        }
    }
}
