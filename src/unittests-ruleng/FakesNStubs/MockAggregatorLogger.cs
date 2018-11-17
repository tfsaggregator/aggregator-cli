using System;
using System.Collections.Generic;
using System.Text;
using aggregator;

namespace aggregator.unittests
{
    class MockLogEntry
    {
        internal string Level { get; set; }
        internal string Message { get; set; }
    }

    class MockAggregatorLogger : IAggregatorLogger
    {
        List<MockLogEntry> log = new List<MockLogEntry>(10);

        public void WriteError(string message)
        {
            log.Add(new MockLogEntry { Level = "Error", Message = message });
        }

        public void WriteInfo(string message)
        {
            log.Add(new MockLogEntry { Level = "Info", Message = message });
        }

        public void WriteVerbose(string message)
        {
            log.Add(new MockLogEntry { Level = "Verbose", Message = message });
        }

        public void WriteWarning(string message)
        {
            log.Add(new MockLogEntry { Level = "Warning", Message = message });
        }

        public MockLogEntry[] GetMessages() => log.ToArray();
    }
}
