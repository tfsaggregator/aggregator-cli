using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    class VstsLogon
    {
        private static readonly string LogonDataTag = "davs";

        public string Url { get; set; }
        public VstsLogonMode Mode { get; set; }
        public string Token { get; set; }

        public string Save()
        {
            return new LogonDataStore(LogonDataTag).Save(this);
        }

        public static VstsLogon Load()
        {
            return new LogonDataStore(LogonDataTag).Load<VstsLogon>();
        }

        public async Task<VssConnection> LogonAsync()
        {
            var clientCredentials = default(VssCredentials);
            switch (Mode)
            {
                case VstsLogonMode.Integrated:
                    clientCredentials = new VssCredentials();
                    break;
                case VstsLogonMode.PAT:
                    clientCredentials=new VssBasicCredential("pat", Token);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode));
            }
            var connection = new VssConnection(new Uri(Url), clientCredentials);
            await connection.ConnectAsync();
            return connection;
        }
    }
}
