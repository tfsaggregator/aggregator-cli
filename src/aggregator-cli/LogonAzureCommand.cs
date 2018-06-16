using CommandLine;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace aggregator.cli
{
    [Verb("logon.azure", HelpText = "Logon into Azure.")]
    class LogonAzureCommand : CommandBase
    {
        [Option('u', "user", Required = true, HelpText = "Username to connect.")]
        public string Username { get; set; }
        [Option('p', "password", Required = true, HelpText = "Password.")]
        public string Password { get; set; }
        [Option('c', "client", Required = true, HelpText = "Client Id.")]
        public string ClientId { get; set; }
        [Option('t', "tenant", Required = true, HelpText = "Tenant Id.")]
        public string TenantId { get; set; }

        override internal int Run()
        {
            string logonAzureDataString = JsonConvert.SerializeObject(this);
            var logonAzureData = UnicodeEncoding.UTF8.GetBytes(logonAzureDataString);

            var entropy = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(entropy);
            var encryptedData = ProtectedData.Protect(logonAzureData, entropy, DataProtectionScope.CurrentUser);

            string logonAzureDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aggregator-cli",
                "daaz.dat");
            // make sure exists
            Directory.CreateDirectory(Path.GetDirectoryName(logonAzureDataPath));

            var magic = new byte[] { 0x44, 0x41, 0x41, 0x5A };
            using (var stream = new FileStream(logonAzureDataPath, FileMode.OpenOrCreate))
            {
                stream.Write(magic, 0, magic.Length);
                stream.Write(entropy, 0, entropy.Length);
                stream.Write(encryptedData, 0, encryptedData.Length);
            }


            // now check for validity


            byte[] outBuffer;
            using (var stream = new FileStream(logonAzureDataPath, FileMode.Open))
            {
                var magicBuffer = new byte[magic.Length];
                stream.Read(magicBuffer, 0, magicBuffer.Length);
                System.Diagnostics.Debug.Assert(magicBuffer == magic);
                var inBuffer = new byte[stream.Length];
                stream.Read(entropy, 0, entropy.Length);
                stream.Read(inBuffer, 0, inBuffer.Length);
                outBuffer = ProtectedData.Unprotect(inBuffer, entropy, DataProtectionScope.CurrentUser);
            }
            var logonAzureDataString2 = UnicodeEncoding.UTF8.GetString(outBuffer);
            var source = JsonConvert.DeserializeObject<LogonAzureCommand>(logonAzureDataString2);
        

            var credentials = SdkContext.AzureCredentialsFactory
                .FromUser(
                    username: source.Username,
                    password: source.Password,
                    clientId: source.ClientId,
                    tenantId: source.TenantId,
                    environment: AzureEnvironment.AzureGlobalCloud);

            // validate credentials
            var azure = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(credentials)
                .WithDefaultSubscription();

            return 2;
        }
    }
}
