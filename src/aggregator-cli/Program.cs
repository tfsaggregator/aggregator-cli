using CommandLine;
using System;

namespace aggregator.cli
{
    class Program
    {
        /*
- list.instances
- install.instance
- unistall.instance
- configure.instance
- list.rules
- add.rule
- remove.rule
- list.mappings
- map.rule
- unmap.rule
- configure.rule
- run.local
         */
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<LogonAzureCommand, LogonVstsCommand>(args)
                .MapResult(
                    (LogonAzureCommand cmd) => cmd.Run(),
                    (LogonVstsCommand cmd) => cmd.Run(),
                    errs => 1);
        }
    }
}
