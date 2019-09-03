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

    public static class AggregatorLoggerExtensions
    {
        public static void WriteVerbose(this IAggregatorLogger logger, string[] messages)
        {
            WriteMultiple(logger.WriteVerbose, messages);
        }
        public static void WriteInfo(this IAggregatorLogger logger, string[] messages)
        {
            WriteMultiple(logger.WriteInfo, messages);
        }

        public static void WriteWarning(this IAggregatorLogger logger, string[] messages)
        {
            WriteMultiple(logger.WriteWarning, messages);
        }

        public static void WriteError(this IAggregatorLogger logger, string[] messages)
        {
            WriteMultiple(logger.WriteError, messages);
        }

        private static void WriteMultiple(Action<string> logAction, string[] messages)
        {
            foreach (var message in messages)
            {
                logAction(message);
            }
        }
    }
}
