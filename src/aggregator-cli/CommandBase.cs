using CommandLine;
using Microsoft.Azure.Management.Fluent;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
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

        protected async Task<(IAzure azure, VssConnection vsts)> Logon<T,U>()
        {
            (IAzure azure, VssConnection vsts) result = default;
            if (typeof(T) == typeof(AzureLogon))
            {
                WriteInfo($"Authenticating to Azure...");
                var logon = AzureLogon.Load();
                if (logon == null)
                {
                    throw new ApplicationException($"No cached Azure credential: use the logon.azure command.");
                }
                result.azure = await logon.LogonAsync();
            }

            if (typeof(U) == typeof(VstsLogon))
            {
                WriteInfo($"Authenticating to VSTS...");
                var logon = VstsLogon.Load();
                if (logon == null)
                {
                    throw new ApplicationException($"No cached VSTS credential: use the logon.vsts command.");
                }
                result.vsts = await logon.LogonAsync();
            }
            return result;
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
                    logger.WriteError("Failed!");
                } else
                {
                    logger.WriteError("Succeeded");
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
