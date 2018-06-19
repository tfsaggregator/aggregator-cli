using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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

        public IAzure Logon()
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
                return Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                    .Authenticate(credentials)
                    .WithSubscription(SubscriptionId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetAuthorizationToken()
        {
            var cc = new ClientCredential(this.ClientId, this.ClientSecret);
            var context = new AuthenticationContext("https://login.windows.net/" + this.TenantId);
            var result = context.AcquireTokenAsync("https://management.azure.com/", cc);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.Result.AccessToken;
        }
    }
}
