using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace aggregator_host
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public const string AuthenticationHeaderName = "X-Auth-ApiKey";

        public IServiceProvider ServiceProvider { get; set; }

        public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IServiceProvider serviceProvider)
            : base(options, logger, encoder, clock)
        {
            ServiceProvider = serviceProvider;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var headers = Request.Headers;
            StringValues apiKey;          
            if (!headers.TryGetValue(AuthenticationHeaderName, out apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Token is null"));
            }

            var repo = (IApiKeyRepository)ServiceProvider.GetService(typeof(IApiKeyRepository));
            if (!repo.IsValidApiKey(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Invalid Api Key '{apiKey}'"));
            }

            var claims = new[] { new Claim("apikey", apiKey) };
            var identity = new ClaimsIdentity(claims, nameof(ApiKeyAuthenticationHandler));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
