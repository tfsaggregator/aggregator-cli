using System;
using System.IO;
using Xunit.Abstractions;

namespace integrationtests.cli
{
    public abstract class End2EndScenarioBase
    {
        private readonly ITestOutputHelper xunitOutput;

        public End2EndScenarioBase(ITestOutputHelper output)
        {
            this.xunitOutput = output;
        }

        protected (int rc, string output) RunAggregatorCommand(string commandLine)
        {
            var args = commandLine.Split(' ');

            var save_out = Console.Out;
            var save_err = Console.Error;
            var buffered = new StringWriter();
            Console.SetOut(buffered);
            Console.SetError(buffered);

            int rc = aggregator.cli.Program.Main(args);

            Console.SetOut(save_out);
            Console.SetError(save_err);

            string output = buffered.ToString();
            xunitOutput.WriteLine(output);

            return (rc, output);
        }
    }
}