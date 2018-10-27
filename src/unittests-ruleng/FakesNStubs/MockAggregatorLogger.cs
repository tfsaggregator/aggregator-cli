using System;
using System.Collections.Generic;
using System.Text;
using aggregator;

namespace aggregator.unittests
{
    class MockAggregatorLogger : IAggregatorLogger
    {
        public void WriteError(string message)
        {
            //no-op
        }

        public void WriteInfo(string message)
        {
            //no-op
        }

        public void WriteVerbose(string message)
        {
            //no-op
        }

        public void WriteWarning(string message)
        {
            //no-op
        }
    }
}
