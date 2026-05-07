using Microsoft.AspNetCore.Components.Authorization;

namespace PurchaseOrderApi.Studio.Providers
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // FOR NOW - Create a fake identity. 
            // Providing a value for "AuthenticationType" (the "FakeAuth" string) 
            // is what makes 'IsAuthenticated' return true.
            var identity = new System.Security.Claims.ClaimsIdentity(new[]
            {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Admin"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
        }, "FakeAuth");

            var user = new System.Security.Claims.ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(user));
        }
    }
}
