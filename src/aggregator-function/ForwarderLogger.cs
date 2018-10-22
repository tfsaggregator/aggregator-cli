using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace aggregator
{
    internal class ForwarderLogger : IAggregatorLogger
    {
        private Microsoft.Extensions.Logging.ILogger log;

        public ForwarderLogger(Microsoft.Extensions.Logging.ILogger log)
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