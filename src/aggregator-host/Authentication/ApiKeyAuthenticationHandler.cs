using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using aggregator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aggregator_host
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public const string AuthenticationHeaderName = MagicConstants.ApiKeyAuthenticationHeaderName;

        public IServiceProvider ServiceProvider { get; set; }

        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IServiceProvider serviceProvider)
            : base(options, logger, encoder, clock)
        {
            ServiceProvider = serviceProvider;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var headers = Request.Headers;
            if (!headers.TryGetValue(AuthenticationHeaderName, out var apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Api Key is null"));
            }

            var repo = (IApiKeyRepository)ServiceProvider.GetService(typeof(IApiKeyRepository));
            if (!repo.IsValidApiKey(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Invalid Api Key '{apiKey}'"));
            }

            var claims = new[] { new Claim("apikey", apiKey) };
#pragma warning disable S4834 // Make sure that permissions are controlled safely here
            var identity = new ClaimsIdentity(claims, nameof(ApiKeyAuthenticationHandler));
#pragma warning restore S4834 // Make sure that permissions are controlled safely here
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
