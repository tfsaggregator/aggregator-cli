using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator
{
    interface ILogger
    {
        void WriteVerbose(string message);

        void WriteInfo(string message);

        void WriteWarning(string message);

        void WriteError(string message);
    }
}
