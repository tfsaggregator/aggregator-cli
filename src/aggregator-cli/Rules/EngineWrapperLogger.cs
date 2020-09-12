namespace aggregator.cli
{
    internal class EngineWrapperLogger : IAggregatorLogger
    {
        private readonly ILogger logger;

        public EngineWrapperLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public void WriteError(string message)
        {
            logger.WriteError(message);
        }

        public void WriteInfo(string message)
        {
            logger.WriteInfo(message);
        }

        public void WriteVerbose(string message)
        {
            logger.WriteVerbose(message);
        }

        public void WriteWarning(string message)
        {
            logger.WriteWarning(message);
        }
    }
}
