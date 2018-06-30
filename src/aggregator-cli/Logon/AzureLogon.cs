using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    class AzureLogon
    {
        private static readonly string LogonDataTag = "daaz";

        public string SubscriptionId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }


        public string Save()
        {
            return new LogonDataStore(LogonDataTag).Save(this);
        }

        public static AzureLogon Load()
        {
            return new LogonDataStore(LogonDataTag).Load<AzureLogon>();
        }

        public async Task<IAzure> LogonAsync()
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(
                        ClientId,
                        ClientSecret,
                        TenantId,
                        AzureEnvironment.AzureGlobalCloud);

                // validate credentials
                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                    .Authenticate(credentials)
                    .WithSubscription(SubscriptionId);
                return azure;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> GetAuthorizationToken()
        {
            var cc = new ClientCredential(this.ClientId, this.ClientSecret);
            var context = new AuthenticationContext("https://login.windows.net/" + this.TenantId);
            var result = await context.AcquireTokenAsync("https://management.azure.com/", cc);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
