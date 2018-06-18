using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace aggregator.cli
{
    class AggregatorRules
    {
        private IAzure azure;

        public AggregatorRules(IAzure azure)
        {
            this.azure = azure;
        }

        internal IEnumerable<(string name, object configuration)> List(string instance)
        {
            string apiUrl = $"https://{instance}.scm.azurewebsites.net/api/functions";
            return null;
        }

        internal async Task AddAsync(string instance, string name, string filePath)
        {
            byte[] zipContent = CreateTemporaryZipForRule(filePath);

            var instances = new AggregatorInstances(azure);
            (string username, string password) = instances.GetPublishCredentials(instance);

            await UploadZipWithRule(instance, zipContent, username, password);
        }

        private static byte[] CreateTemporaryZipForRule(string filePath)
        {
            // see https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push
            var rand = new Random((int)DateTime.UtcNow.Ticks);
            string tempDirPath = Path.Combine(
                Path.GetTempPath(),
                $"aggregator-{rand.Next().ToString()}");
            Directory.CreateDirectory(tempDirPath);
            File.Copy(filePath, Path.Combine(tempDirPath, Path.GetFileName(filePath)));
            string tempZipPath = Path.GetTempFileName();
            File.Delete(tempZipPath);

            ZipFile.CreateFromDirectory(tempDirPath, tempZipPath);
            var zipContent = File.ReadAllBytes(tempZipPath);

            // clean-up: everything is in memory
            Directory.Delete(tempDirPath, true);
            File.Delete(tempZipPath);
            return zipContent;
        }

        private static async Task UploadZipWithRule(string instance, byte[] zipContent, string username, string password)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            string apiUrl = $"https://{instance}.scm.azurewebsites.net/api/zipdeploy";
            string base64AuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(($"{username}:{password}")));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64AuthInfo);
            var body = new ByteArrayContent(zipContent);
            await client.PostAsync(apiUrl, body);
        }
    }
}
