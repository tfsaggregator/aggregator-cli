using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    internal class KuduApi
    {
        private readonly InstanceName instance;
        private readonly IAzure azure;
        private readonly ILogger logger;

        internal KuduApi(InstanceName instance, IAzure azure, ILogger logger)
        {
            this.instance = instance;
            this.azure = azure;
            this.logger = logger;
        }

        string lastPublishCredentialsInstance = string.Empty;
        (string username, string password) lastPublishCredentials = default;
        private async Task<(string username, string password)> GetPublishCredentials()
        {
            // implements a trivial caching, adequate for command line use
            if (lastPublishCredentialsInstance != instance.PlainName)
            {
                string rg = instance.ResourceGroupName;
                string fn = instance.FunctionAppName;
                IFunctionApp webFunctionApp = null;
                try
                {
                    webFunctionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(rg, fn);
                }
                catch (Exception)
                {
                    logger.WriteError($"Instance {instance.PlainName} not found.");
                    throw;
                }
                var ftpUsername = webFunctionApp.GetPublishingProfile().FtpUsername;
                var username = ftpUsername.Split('\\').ToList()[1];
                var password = webFunctionApp.GetPublishingProfile().FtpPassword;

                lastPublishCredentials = (username, password);
                lastPublishCredentialsInstance = instance.PlainName;
            }
            return lastPublishCredentials;
        }

        private async Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            (string username, string password) = await GetPublishCredentials();
            var base64Auth = Convert.ToBase64String(Encoding.Default.GetBytes($"{username}:{password}"));
            return new AuthenticationHeaderValue("Basic", base64Auth);
        }

        internal async Task<string> GetAzureFunctionJWTAsync()
        {
            var kuduUrl = $"{instance.KuduUrl}/api";
            string JWT;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
                client.DefaultRequestHeaders.Authorization = await GetAuthenticationHeader();

                var result = await client.GetAsync($"{kuduUrl}/functions/admin/token");
                JWT = await result.Content.ReadAsStringAsync(); //get  JWT for call function key
                JWT = JWT.Trim('"');
            }
            return JWT;
        }

        internal async Task<HttpRequestMessage> GetRequestAsync(HttpMethod method, string restApi)
        {
            var kuduUrl = new Uri(instance.KuduUrl);
            var request = new HttpRequestMessage(method, $"{kuduUrl}{restApi}");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            request.Headers.Authorization = await GetAuthenticationHeader();
            return request;
        }
    }
}
