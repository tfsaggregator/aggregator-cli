using CommandLine;
using System;

namespace aggregator.cli
{
    class Program
    {
        /*
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
            return Parser.Default.ParseArguments<LogonAzureCommand, LogonVstsCommand, ListInstancesCommand>(args)
                .MapResult(
                    (LogonAzureCommand cmd) => cmd.Run(),
                    (LogonVstsCommand cmd) => cmd.Run(),
                    (ListInstancesCommand cmd) => cmd.Run(),
                    errs => 1);
        }
    }
}
