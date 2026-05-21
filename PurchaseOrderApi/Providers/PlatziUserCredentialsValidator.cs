using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using System.Text.Json.Serialization;

namespace PurchaseOrderApi.Providers
{
    public class PlatziUserCredentialsValidator : IUserCredentialsValidator
    {
        private readonly HttpClient _http;

        public PlatziUserCredentialsValidator(IHttpClientFactory factory)
            => _http = factory.CreateClient("platzi");

        public async ValueTask<User?> ValidateAsync(
            string username, string password, CancellationToken ct = default)
        {
            // 1. Login to Platzi  
            var loginResp = await _http.PostAsJsonAsync(
                "https://api.escuelajs.co/api/v1/auth/login",
                new { email = username, password });

            if (!loginResp.IsSuccessStatusCode) return null;

            var tokens = await loginResp.Content.ReadFromJsonAsync<PlatziTokenResponse>(cancellationToken: ct);
            if (tokens?.AccessToken == null) return null;

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            var profileResp = await _http.GetAsync(
                "https://api.escuelajs.co/api/v1/auth/profile", ct);

            if (!profileResp.IsSuccessStatusCode) return null;

            var profile = await profileResp.Content.ReadFromJsonAsync<PlatziProfile>(cancellationToken: ct);

            var elsaRoleId = profile?.Role switch
            {
                "admin" => "admin",
                "customer" => "viewer",
                _ => "viewer"
            };

            return new User
            {
                Id = profile!.Email,
                Name = profile.Email,
                Roles = new List<string> { elsaRoleId }
            };
        }

        private record PlatziTokenResponse(
            [property: JsonPropertyName("access_token")] string AccessToken,
            [property: JsonPropertyName("refresh_token")] string RefreshToken);

        private record PlatziProfile(
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("role")] string Role);
    }
}
