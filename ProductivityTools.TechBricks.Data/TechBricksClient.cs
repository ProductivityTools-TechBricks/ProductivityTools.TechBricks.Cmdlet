using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ProductivityTools.TechBricks.Data.Auth;
using ProductivityTools.TechBricks.Data.Models;

namespace ProductivityTools.TechBricks.Data
{
    /// <summary>
    /// Client of the TechBricks API (ProductivityTools.TechBricks.Api).
    /// </summary>
    public class TechBricksClient
    {
        private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri(TechBricksConfig.ApiBaseUrl) };

        private readonly AuthService authService;

        public TechBricksClient(AuthService authService)
        {
            this.authService = authService;
        }

        /// <summary>
        /// Returns pallets of the given owner (gmail login, default: the signed
        /// in user) which the signed in user co-owns.
        /// </summary>
        public async Task<List<Pallet>> GetPalletsAsync(string? owner, CancellationToken cancellationToken)
        {
            string path = "pallet";
            if (!string.IsNullOrEmpty(owner))
            {
                path += "?owner=" + Uri.EscapeDataString(owner);
            }
            string body = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);
            return JsonSerializer.Deserialize<List<Pallet>>(body) ?? new List<Pallet>();
        }

        private async Task<string> SendAsync(Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
        {
            using var response = await SendWithTokenAsync(createRequest, forceRefresh: false, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // The token may have been revoked or expired in between - refresh it and retry once.
                using var retryResponse = await SendWithTokenAsync(createRequest, forceRefresh: true, cancellationToken);
                return await ReadAsync(retryResponse, cancellationToken);
            }
            return await ReadAsync(response, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendWithTokenAsync(Func<HttpRequestMessage> createRequest, bool forceRefresh, CancellationToken cancellationToken)
        {
            string idToken = await authService.GetIdTokenAsync(forceRefresh, cancellationToken);
            using var request = createRequest();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await HttpClient.SendAsync(request, cancellationToken);
        }

        private static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"TechBricks API returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}", null, response.StatusCode);
            }
            return body;
        }
    }
}
