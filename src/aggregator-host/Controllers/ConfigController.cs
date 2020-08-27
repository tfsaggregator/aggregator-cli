using aggregator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace aggregator_host.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationScheme.DefaultScheme)]
    public class ConfigController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IConfiguration _configuration;
        IApiKeyRepository _apiKeyRepo;

        public ConfigController(IConfiguration configuration, ILoggerFactory loggerFactory, IApiKeyRepository apiKeyRepo)
        {
            _configuration = configuration;
            _log = loggerFactory.CreateLogger(MagicConstants.LoggerCategoryName);
            _apiKeyRepo = apiKeyRepo;
        }

        [HttpGet(("{proof}"))]
        [AllowAnonymous] // bootstrap
        public string GetKey(string proof)
        {
            _log.LogDebug("GetKey method was called!");

            if (proof== MagicConstants.SharedSecret)
            {
                return _apiKeyRepo.PickValidKey();
            }

            return "OK";

        }
    }
}
