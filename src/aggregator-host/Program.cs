using System;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using aggregator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace aggregator_host
{
    public static class Program
    {
        private static bool InDocker
            => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public static async System.Threading.Tasks.Task Main(string[] args)
        {
            if (InDocker)
            {
                Console.WriteLine($@"Aggregator {RequestHelper.AggregatorVersion} started in Docker mode.
{RuntimeInformation.FrameworkDescription}
{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()})
");

                Telemetry.InitializeTelemetry();
                Telemetry.TrackEvent("Docker Host Start");

                var host = CreatePlainHostBuilder(args).Build();
                //HACK https://stackoverflow.com/a/56079178
                var repo = (IApiKeyRepository)host.Services.GetService(typeof(IApiKeyRepository));
                await repo.LoadAsync();
                await host.RunAsync();

                Telemetry.TrackEvent("Docker Host End");
                Telemetry.Shutdown();
            }
            else
            {
                Console.WriteLine("Unsupported run mode, exiting.");
            }
        }

        public static IHostBuilder CreatePlainHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureWebHost(config =>
                {
                    config
                        .ConfigureKestrel(serverOptions =>
                        {
                            /* Sample PowerShell to generate a self-signed certificate
                             * $cert = New-SelfSignedCertificate -KeyLength 2048 -KeyAlgorithm RSA -Type SSLServerAuthentication -FriendlyName "Aggregator" -NotAfter 2025-12-31 -Subject "localhost" -TextExtension @("2.5.29.17={text}DNS=localhost&IPAddress=127.0.0.1&IPAddress=::1")
                             * $certPass = Read-Host -Prompt "Cert pass" -AsSecureString
                             * Export-PfxCertificate -FilePath "aggregator-localhost.pfx" -Cert $cert -Password $certPass
                             */
                            serverOptions.ConfigureHttpsDefaults(listenOptions =>
                            {
                                listenOptions.SslProtocols = SslProtocols.Tls12;
                            });
                        })
                        ;
                })
                ;
    }
}
