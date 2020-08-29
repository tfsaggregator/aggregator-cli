namespace aggregator.cli
{
    interface ILogger
    {
        void WriteOutput(ILogDataObject data);

        void WriteVerbose(string message);

        void WriteInfo(string message);

        void WriteSuccess(string message);

        void WriteWarning(string message);

        void WriteError(string message);
    }
}
