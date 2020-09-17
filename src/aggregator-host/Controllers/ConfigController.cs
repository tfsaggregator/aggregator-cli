﻿using System;
using System.Text.Json;
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
        private readonly IApiKeyRepository _apiKeyRepo;

        public ConfigController(IConfiguration configuration, ILoggerFactory loggerFactory, IApiKeyRepository apiKeyRepo)
        {
            _configuration = configuration;
            _log = loggerFactory.CreateLogger(MagicConstants.LoggerCategoryName);
            _apiKeyRepo = apiKeyRepo;
        }

        [HttpGet("status")]
        [AllowAnonymous] // bootstrap
        public string GetStatus()
        {
            _log.LogInformation("GetStatus method was called!");
            return "OK";

        }

        [HttpGet("version")]
        [AllowAnonymous] // bootstrap
        public string GetVersion()
        {
            _log.LogInformation("GetVersion method was called!");
            var aggregatorVersion = RequestHelper.AggregatorVersion;
            return aggregatorVersion;

        }
        [HttpPost("key")]
        [AllowAnonymous] // bootstrap
        public string RetrieveKey([FromBody] JsonElement body)
        {
            _log.LogDebug("RetrieveKey method was called!");

            string proof = body.GetString();
            string userManagedPassword = _configuration.GetValue<string>(MagicConstants.EnvironmentVariable_SharedSecret);
            if (string.IsNullOrEmpty(userManagedPassword))
            {
                throw new ApplicationException($"{MagicConstants.EnvironmentVariable_SharedSecret} environment variable is required by CLI");
            }

            if (proof == SharedSecret.DeriveFromPassword(userManagedPassword))
            {
                return _apiKeyRepo.PickValidKey();
            }
            else
            {
                _log.LogError("API Key request failed from {0}", HttpContext.Connection.RemoteIpAddress);
                return MagicConstants.InvalidApiKey;

            }
        }
    }
}
