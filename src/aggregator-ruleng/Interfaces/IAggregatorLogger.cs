namespace aggregator
{
    public interface IAggregatorLogger
    {
        void WriteVerbose(string message);

        void WriteInfo(string message);

        void WriteWarning(string message);

        void WriteError(string message);
    }

    /// <summary>
    /// an emtpy logger implementation
    /// </summary>
    public class NullLogger : IAggregatorLogger
    {
        /// <inheritdoc />
        public void WriteVerbose(string message)
        {
            // must be empty
        }


        /// <inheritdoc />
        public void WriteInfo(string message)
        {
            // must be empty
        }


        /// <inheritdoc />
        public void WriteWarning(string message)
        {
            // must be empty
        }


        /// <inheritdoc />
        public void WriteError(string message)
        {
            // must be empty
        }
    }
}
