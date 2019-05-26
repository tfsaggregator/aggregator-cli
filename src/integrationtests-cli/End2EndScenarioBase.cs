using System;
using System.IO;
using Xunit.Abstractions;

namespace integrationtests.cli
{
    public abstract class End2EndScenarioBase
    {
        static protected TestLogonData TestLogonData = new TestLogonData("logon-data.json");

        private readonly ITestOutputHelper _output;

        protected End2EndScenarioBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected (int rc, string output) RunAggregatorCommand(string commandLine)
        {
            var args = commandLine.Split(' ');

            var saveOut = Console.Out;
            var saveErr = Console.Error;
            var buffered = new StringWriter();
            Console.SetOut(buffered);
            Console.SetError(buffered);

            var rc = aggregator.cli.Program.Main(args);

            Console.SetOut(saveOut);
            Console.SetError(saveErr);

            var output = buffered.ToString();
            _output.WriteLine(output);

            return (rc, output);
        }
    }
}