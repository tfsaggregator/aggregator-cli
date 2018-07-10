using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    interface ILogger
    {
        void WriteOutput(object data, Func<object, string> humanOutput);

        void WriteVerbose(string message);

        void WriteInfo(string message);

        void WriteSuccess(string message);

        void WriteWarning(string message);

        void WriteError(string message);
    }
}
