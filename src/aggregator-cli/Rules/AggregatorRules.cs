using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

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

        internal async System.Threading.Tasks.Task AddAsync(string instance, string name, string filePath)
        {
            // create temp Zip
            // see https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push
            var rand = new Random((int)DateTime.UtcNow.Ticks);
            string tempDirPath = Path.Combine(
                Path.GetTempPath(),
                $"aggregator-{rand.Next().ToString()}");
            Directory.CreateDirectory(tempDirPath);
            File.Copy(filePath, Path.Combine(tempDirPath,Path.GetFileName(filePath)));
            string tempZipPath = Path.GetTempFileName();
            File.Delete(tempZipPath);
            ZipFile.CreateFromDirectory(tempDirPath, tempZipPath);
            var zipContent = File.ReadAllBytes(tempZipPath);
            // clean-up: everything is in memory
            Directory.Delete(tempDirPath, true);
            File.Delete(tempZipPath);

            // get publish credentials
            // see https://zimmergren.net/azure-resource-manager-part-7-download-an-azure-publishing-profile-xml-programmatically-using-rest/
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetAuthorizationToken());
            string apiUrl = $"https://management.azure.com/subscriptions/{azure.SubscriptionId}/resourceGroups/aggregator-{instance}/providers/Microsoft.Web/sites/{instance}/publishxml?api-version=2016-08-01";
            var response = client.PostAsync(apiUrl, new StringContent(string.Empty));
            string publishxml = await response.Result.Content.ReadAsStringAsync();
            var publishDoc = new XmlDocument();
            publishDoc.LoadXml(publishxml);
            var node = publishDoc.SelectSingleNode("/publishData/publishProfile[@publishMethod='MSDeploy']");
            string username = node.Attributes["userName"].Value;
            string password = node.Attributes["userPWD"].Value;

            // upload
            apiUrl = $"https://{instance}.scm.azurewebsites.net/api/zipdeploy";
            string base64AuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(($"{username}:{password}")));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64AuthInfo);
            var body = new ByteArrayContent(zipContent);
            await client.PostAsync(apiUrl, body);
        }

        private string GetAuthorizationToken()
        {
            // HACK
            string thePassword = "******";
            var ac = azure.AppServices.RestClient.Credentials;

            var cc = new ClientCredential(ac.ClientId, thePassword);
            var context = new AuthenticationContext("https://login.windows.net/" + ac.TenantId);
            var result = context.AcquireTokenAsync("https://management.azure.com/", cc);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.Result.AccessToken;
        }
    }
}
