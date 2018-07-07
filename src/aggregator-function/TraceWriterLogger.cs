using Microsoft.Azure.WebJobs.Host;

namespace aggregator
{
    internal class TraceWriterLogger : ILogger
    {
        private TraceWriter log;

        public TraceWriterLogger(TraceWriter log)
        {
            this.log = log;
        }

        public void WriteError(string message)
        {
            log.Error(message);
        }

        public void WriteInfo(string message)
        {
            log.Info(message);
        }

        public void WriteVerbose(string message)
        {
            log.Verbose(message);
        }

        public void WriteWarning(string message)
        {
            log.Warning(message);
        }
    }
}