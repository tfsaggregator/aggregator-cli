using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;


namespace aggregator.cli
{
    class DevOpsLogon : LogonDataBase
    {
        private static readonly string LogonDataTag = "davs";

        public string Url { get; set; }
        public DevOpsTokenType Mode { get; set; }
        public string Token { get; set; }

        public void Clear()
        {
            new LogonDataStore(LogonDataTag).Clear();
        }

        public string Save()
        {
            return new LogonDataStore(LogonDataTag).Save(this);
        }

        public static (DevOpsLogon connection, LogonResult reason) Load()
        {
            (DevOpsLogon connection, LogonResult reason) = new LogonDataStore(LogonDataTag).Load<DevOpsLogon>();
            return (connection, reason);
        }

        public async Task<VssConnection> LogonAsync(CancellationToken cancellationToken)
        {
            VssCredentials clientCredentials;
            switch (Mode)
            {
                case DevOpsTokenType.Integrated:
                    clientCredentials = new VssCredentials();
                    break;
                case DevOpsTokenType.PAT:
                    clientCredentials = new VssBasicCredential("pat", Token);
                    break;
                default:
                    throw new InvalidOperationException($"BUG: Unexpected value {Mode} for {nameof(Mode)}");
            }

            // see https://rules.sonarsource.com/csharp/RSPEC-4457
            return await LocalAsyncImpl(clientCredentials, cancellationToken);
        }

        private async Task<VssConnection> LocalAsyncImpl(VssCredentials clientCredentials, CancellationToken cancellationToken)
        {
            var connection = new VssConnection(new Uri(Url), clientCredentials);
            await connection.ConnectAsync(cancellationToken);
            return connection;
        }
    }
}
