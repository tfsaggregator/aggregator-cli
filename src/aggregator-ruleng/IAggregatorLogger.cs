using System;
using System.Collections.Generic;
using System.Text;

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
        }


        /// <inheritdoc />
        public void WriteInfo(string message)
        {
        }


        /// <inheritdoc />
        public void WriteWarning(string message)
        {
        }


        /// <inheritdoc />
        public void WriteError(string message)
        {
        }
    }
}
