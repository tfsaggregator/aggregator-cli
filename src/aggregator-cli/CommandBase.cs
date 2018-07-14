using CommandLine;
using Microsoft.Azure.Management.Fluent;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace aggregator.cli
{
    abstract class CommandBase
    {
        ILogger logger = null;

        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        protected ContextBuilder Context => new ContextBuilder(logger);

        internal abstract Task<int> RunAsync();

        internal int Run()
        {
            this.logger = new ConsoleLogger(Verbose);
            try
            {
                var title = GetCustomAttribute<AssemblyTitleAttribute>();
                var config = GetCustomAttribute<AssemblyConfigurationAttribute>();
                var fileVersion = GetCustomAttribute<AssemblyFileVersionAttribute>();
                var infoVersion = GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var copyright = GetCustomAttribute<AssemblyCopyrightAttribute>();

                // Hello World
                logger.WriteInfo($"{title.Title} v{infoVersion.InformationalVersion} (build: {fileVersion.Version} {config.Configuration}) (c) {copyright.Copyright}");

                var t = this.RunAsync();
                t.Wait();
                int rc = t.Result;
                if (rc != 0)
                {
                    logger.WriteError("Failed!");
                } else
                {
                    logger.WriteSuccess("Succeeded");
                }
                return rc;
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
                return 99;
            }
        }

        private static T GetCustomAttribute<T>()
            where T : Attribute
        {
            return Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }
    }
}
