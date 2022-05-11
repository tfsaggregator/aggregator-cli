using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Xunit.Abstractions;

namespace integrationtests.cli
{
    public abstract class End2EndScenarioBase
    {
        protected static TestLogonData TestLogonData = new(
            // CI scenario
            Environment.GetEnvironmentVariable("DOWNLOADSECUREFILE_SECUREFILEPATH")
            // Visual Studio
            ?? "logon-data.json");

        private readonly ITestOutputHelper _output;

        protected End2EndScenarioBase(ITestOutputHelper output)
        {
            _output = output;
        }

        protected void WriteLineToOutput(string message)
        {
            _output.WriteLine(message);
        }

        protected async Task<(int rc, string output)> RunAggregatorCommand(string commandLine, IEnumerable<(string, string)> env = default)
        {
            // see https://stackoverflow.com/a/14655145/100864
            var args = Regex.Matches(commandLine, @"[\""](?<a>.+?)[\""]|(?<a>[^ ]+)")
                .Cast<Match>()
                .Select(m => m.Groups["a"].Value)
                .ToArray();

            var saveOut = Console.Out;
            var saveErr = Console.Error;
            var buffered = new StringWriter();
            Console.SetOut(buffered);
            Console.SetError(buffered);

            if (env != default)
            {
                env.ForEach((pair) =>
                {
                    (string name, string value) = pair;
                    Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
                });
            }

            var rc = await aggregator.cli.Program.Main(args);

            Console.SetOut(saveOut);
            Console.SetError(saveErr);

            var output = buffered.ToString();
            _output.WriteLine(output);

            return (rc, output);
        }

        protected async Task<(int rc, string output)> RunAggregatorProcess(string exeDirectory, string arguments, IEnumerable<(string, string)> env = default)
        {
            string exeName = "aggregator-cli";
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                exeName += ".exe";
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Path.GetFullPath(exeDirectory), exeName),
                    Arguments = arguments,
                    WorkingDirectory = exeDirectory,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };
            if (p.Start())
            {
                await p.WaitForExitAsync();
                var buf = new System.Text.StringBuilder();
                while (!p.StandardOutput.EndOfStream)
                {
                    string line = p.StandardOutput.ReadLine();
                    buf.AppendLine(line);
                    _output.WriteLine(line);
                }
                while (!p.StandardError.EndOfStream)
                {
                    string line = p.StandardError.ReadLine();
                    buf.AppendLine(line);
                    _output.WriteLine(line);
                }
                return (p.ExitCode, buf.ToString());
            }
            return (99, string.Empty);
        }

        protected async Task DownloadFile(string sourceUrl, string destinationFile, CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    _output.WriteLine($"Downloading file from {sourceUrl}");
                    using (var fileStream = File.Create(destinationFile))
                    {
                        await response.Content.CopyToAsync(fileStream, cancellationToken);
                    }
                    _output.WriteLine($"File downloaded.");
                    break;

                default:
                    _output.WriteLine($"{sourceUrl} returned {response.ReasonPhrase}.");
                    break;
            }//switch
        }
    }
}
