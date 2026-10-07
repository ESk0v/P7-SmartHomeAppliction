using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace SmartHomeApplication.Services
{
    public class AuthenticationProvider : AuthenticationStateProvider
    {
        //private readonly ILogger<AuthenticationProvider> _logger; --> kommer ind igen senere
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

        }
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var empty = new ClaimsPrincipal(new ClaimsIdentity());
            var user = _httpContextAccessor.HttpContext?.User ?? empty;

            return Task.FromResult(new AuthenticationState(user));
        }
    }
}
