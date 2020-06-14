﻿using CommandLine;
using Microsoft.Azure.Management.Fluent;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    abstract class CommandBase
    {
        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option("namingTemplate", Default = "", HelpText = "Define template-set for generating names of Azure Resources.")]
        public string NamingTemplate { get; set; }

        protected ContextBuilder Context => new ContextBuilder(Logger, this.NamingTemplate);

        internal ILogger Logger { get; private set; }

        internal abstract Task<int> RunAsync(CancellationToken cancellationToken);

        internal int Run(CancellationToken cancellationToken)
        {
            Logger = new ConsoleLogger(Verbose);
            try
            {
                var title = GetCustomAttribute<AssemblyTitleAttribute>();
                var config = GetCustomAttribute<AssemblyConfigurationAttribute>();
                var fileVersion = GetCustomAttribute<AssemblyFileVersionAttribute>();
                var infoVersion = GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var copyright = GetCustomAttribute<AssemblyCopyrightAttribute>();

                // Hello World
                Logger.WriteInfo($"{title.Title} v{infoVersion.InformationalVersion} (build: {fileVersion.Version} {config.Configuration}) (c) {copyright.Copyright}");

                var t = RunAsync(cancellationToken);
                t.Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                int rc = t.Result;
                if (rc != 0)
                {
                    Logger.WriteError("Failed!");
                }
                else
                {
                    Logger.WriteSuccess("Succeeded");
                }

                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteError(
                    ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.Message
                    );
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
