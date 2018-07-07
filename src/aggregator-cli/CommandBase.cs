using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    abstract class CommandBase : ILogger
    {
        ILogger logger = new ConsoleLogger();

        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        internal abstract Task<int> RunAsync();

        public void WriteOutput(object data, Func<object, string> humanOutput)
        {
            logger.WriteOutput(data, humanOutput);
        }

        public void WriteVerbose(string message)
        {
            if (!Verbose)
                return;
            logger.WriteVerbose(message);
        }

        public void WriteInfo(string message)
        {
            logger.WriteInfo(message);
        }

        public void WriteWarning(string message)
        {
            logger.WriteWarning(message);
        }

        public void WriteError(string message)
        {
            logger.WriteError(message);
        }
    }

    static class CommandBaseExtension
    {
        static internal int Run(this CommandBase cmd)
        {
            try
            {
                var t = cmd.RunAsync();
                t.Wait();
                int rc = t.Result;
                var logger = new ConsoleLogger();
                if (rc != 0)
                {
                    logger.WriteError("Failed");
                }
                return rc;
            }
            catch (Exception ex)
            {
                var logger = new ConsoleLogger();
                logger.WriteError(ex.Message);
                return 99;
            }
        }
    }
}
