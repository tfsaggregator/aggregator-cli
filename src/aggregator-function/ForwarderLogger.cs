using Microsoft.Extensions.Logging;

namespace aggregator
{
    /// <summary>
    /// Forwards to Azure Function logging subsystem
    /// </summary>
    internal class ForwarderLogger : IAggregatorLogger
    {
        private readonly ILogger log;

        public ForwarderLogger(ILogger log)
        {
            this.log = log;
        }

        public void WriteError(string message)
        {
            log.LogError(message);
        }

        public void WriteInfo(string message)
        {
            log.LogInformation(message);
        }

        public void WriteVerbose(string message)
        {
            log.LogDebug(message);
        }

        public void WriteWarning(string message)
        {
            log.LogWarning(message);
        }
    }
}