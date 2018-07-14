using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    class ConsoleLogger : ILogger
    {
        private readonly bool verbose;

        public ConsoleLogger(bool verbose)
        {
            this.verbose = verbose;
        }

        protected void WriteMessagePrefix(System.IO.TextWriter w)
        {
            w.Write($"[{DateTime.Now.ToString("u")}] ");
        }
        public void WriteOutput(object data, Func<object, string> humanOutput)
        {
            string message = humanOutput(data);
            Console.WriteLine(message);
        }

        public void WriteVerbose(string message)
        {
            if (!verbose) { return; }

            var save = Console.ForegroundColor;
            WriteMessagePrefix(Console.Out);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
        }

        public void WriteInfo(string message)
        {
            WriteMessagePrefix(Console.Out);
            Console.WriteLine(message);
        }

        public void WriteSuccess(string message)
        {
            var save = Console.ForegroundColor;
            WriteMessagePrefix(Console.Out);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
        }

        public void WriteWarning(string message)
        {
            var save = Console.ForegroundColor;
            WriteMessagePrefix(Console.Error);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = save;
        }

        public void WriteError(string message)
        {
            var save = Console.ForegroundColor;
            WriteMessagePrefix(Console.Error);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = save;
        }
    }
}
