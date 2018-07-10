using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    class ConsoleLogger : ILogger
    {
        public void WriteOutput(object data, Func<object, string> humanOutput)
        {
            string message = humanOutput(data);
            Console.WriteLine(message);
        }

        public void WriteVerbose(string message)
        {
            var save = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
        }

        public void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteSuccess(string message)
        {
            var save = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
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
