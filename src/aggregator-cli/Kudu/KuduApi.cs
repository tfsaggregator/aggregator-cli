﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;

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
        (string username, string password) lastPublishCredentials;
        private async Task<(string username, string password)> GetPublishCredentials(CancellationToken cancellationToken)
        {
            // implements a trivial caching, adequate for command line use
            if (lastPublishCredentialsInstance != instance.PlainName)
            {
                string rg = instance.ResourceGroupName;
                string fn = instance.FunctionAppName;
                IFunctionApp webFunctionApp;
                try
                {
                    webFunctionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(rg, fn, cancellationToken);
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

        private async Task<AuthenticationHeaderValue> GetAuthenticationHeader(CancellationToken cancellationToken)
        {
            var (username, password) = await GetPublishCredentials(cancellationToken);
            var base64Auth = Convert.ToBase64String(Encoding.Default.GetBytes($"{username}:{password}"));
            return new AuthenticationHeaderValue("Basic", base64Auth);
        }

        internal async Task<string> GetAzureFunctionJWTAsync(CancellationToken cancellationToken)
        {
            var kuduUrl = $"{instance.KuduUrl}/api";
            string JWT;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
                client.DefaultRequestHeaders.Authorization = await GetAuthenticationHeader(cancellationToken);

                var result = await client.GetAsync($"{kuduUrl}/functions/admin/token", cancellationToken);
                JWT = await result.Content.ReadAsStringAsync(); //get  JWT for call function key
                JWT = JWT.Trim('"');
            }
            return JWT;
        }

        internal async Task<HttpRequestMessage> GetRequestAsync(HttpMethod method, string restApi, CancellationToken cancellationToken)
        {
            var kuduUrl = new Uri(instance.KuduUrl);
            var request = new HttpRequestMessage(method, $"{kuduUrl}{restApi}");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            request.Headers.Authorization = await GetAuthenticationHeader(cancellationToken);
            return request;
        }

        internal async Task StreamLogsAsync(TextWriter output, string lastLinePattern, CancellationToken cancellationToken)
        {
            var regex = new Regex(lastLinePattern);

            // see https://github.com/projectkudu/kudu/wiki/Diagnostic-Log-Stream
            using (var client = new HttpClient())
            using (var request = await GetRequestAsync(HttpMethod.Get, $"api/logstream/application", cancellationToken))
            {
                logger.WriteInfo($"Connected to {instance.PlainName} logs");
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    logger.WriteVerbose($"Streaming {instance.PlainName} logs...");
                    var stream = await response.Content.ReadAsStreamAsync();

                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            //We are ready to read the stream
                            var line = await reader.ReadLineAsync();

                            if (regex.IsMatch(line))
                                break;

                            await output.WriteLineAsync(line);
                        }
                    }
                }
            }
        }
    }
}
