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
}
