using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace aggregator_host
{
    public interface IApiKeyRepository
    {
        Task LoadAsync();
        bool IsValidApiKey(StringValues reqkey);
    }

    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly ILogger _log;
        private readonly IConfiguration _configuration;
        ApiKeyRecord[] apiKeyRecords;

        public ApiKeyRepository(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _log = loggerFactory.CreateLogger(MagicConstants.LoggerCategoryName);
        }

        public async Task LoadAsync()
        {
            string fileName = _configuration.GetValue<string>("Aggregator_ApiKeysPath");
            _log.LogDebug($"Loading API Keys from {fileName}");
            using (var fs = File.OpenRead(fileName))
            {
                apiKeyRecords = await JsonSerializer.DeserializeAsync<ApiKeyRecord[]>(fs,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            _log.LogInformation($"Loaded {apiKeyRecords?.Length} API Keys");
        }

        public bool IsValidApiKey(StringValues request)
        {
            string reqkey = request.ToString().ToLowerInvariant();

            return apiKeyRecords.Where(rec => rec.Key == reqkey).Any();
        }
    }
}
