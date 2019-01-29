using System;

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
            Console.ForegroundColor
                = Console.BackgroundColor == ConsoleColor.Black
                ? ConsoleColor.DarkMagenta
                : ConsoleColor.DarkGray;
            w.Write($"[{DateTime.Now:u}] ");
        }

        public void WriteOutput(ILogDataObject data)
        {
            string message = data.AsHumanReadable();
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
            var save = Console.ForegroundColor;
            WriteMessagePrefix(Console.Out);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
            Console.ForegroundColor = save;
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
