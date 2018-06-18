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
        private static readonly byte[] magic = new byte[] { 0x44, 0x41, 0x41, 0x5A };

        public string SubscriptionId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        protected static byte[] Magic { get => magic; }

        protected static string LogonAzureDataPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "aggregator-cli",
                    "daaz.dat");
            }
        }

        public string Save()
        {
            string logonAzureDataString = JsonConvert.SerializeObject(this);
            var logonAzureData = UnicodeEncoding.UTF8.GetBytes(logonAzureDataString);

            var entropy = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(entropy);
            var encryptedData = ProtectedData.Protect(logonAzureData, entropy, DataProtectionScope.CurrentUser);

            string logonAzureDataPath = LogonAzureDataPath;
            // make sure exists
            Directory.CreateDirectory(Path.GetDirectoryName(logonAzureDataPath));

            using (var stream = new FileStream(logonAzureDataPath, FileMode.OpenOrCreate))
            {
                stream.Write(Magic, 0, Magic.Length);
                stream.Write(entropy, 0, entropy.Length);
                stream.Write(encryptedData, 0, encryptedData.Length);
            }

            return logonAzureDataPath;
        }

        public static AzureLogon Load()
        {
            if (!File.Exists(LogonAzureDataPath))
                return null;

            var entropy = new byte[16];
            byte[] outBuffer;
            using (var stream = new FileStream(LogonAzureDataPath, FileMode.Open))
            {
                var magicBuffer = new byte[Magic.Length];
                stream.Read(magicBuffer, 0, magicBuffer.Length);
                if (magicBuffer == Magic)
                    throw new InvalidDataException("Invalud credential file");
                var inBuffer = new byte[stream.Length];
                stream.Read(entropy, 0, entropy.Length);
                stream.Read(inBuffer, 0, inBuffer.Length);
                outBuffer = ProtectedData.Unprotect(inBuffer, entropy, DataProtectionScope.CurrentUser);
            }
            var logonAzureDataString = UnicodeEncoding.UTF8.GetString(outBuffer);
            var result = JsonConvert.DeserializeObject<AzureLogon>(logonAzureDataString);
            return result;
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
