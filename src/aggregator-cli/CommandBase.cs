using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    abstract class CommandBase
    {
        // Omitting long name, defaults to name of property, ie "--verbose"
        [Option(Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        internal abstract int Run();
    }
}
