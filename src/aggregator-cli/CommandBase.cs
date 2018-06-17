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

        public void WriteOutput(object data, Func<object, string> humanOutput)
        {
            string message = humanOutput(data);
            Console.WriteLine(message);
        }

        public void WriteVerbse(string message)
        {
            if (!Verbose)
                return;
            Console.WriteLine(message);
        }

        public void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteWarning(string message)
        {
            var save = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
        }

        public void WriteError(string message)
        {
            var save = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
        }
    }
}
