﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
                    logger.WriteVerbose($"Retrieving Kudu publish credentials for {instance.PlainName}.");
                    webFunctionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(rg, fn, cancellationToken);
                }
                catch (Exception)
                {
                    logger.WriteError($"Instance {instance.PlainName} not found (Kudu publish credentials).");
                    throw;
                }

                var ftpUsername = webFunctionApp.GetPublishingProfile().FtpUsername;
                var username = ftpUsername.Split('\\').ToList()[1];
                var password = webFunctionApp.GetPublishingProfile().FtpPassword;

                lastPublishCredentials = (username, password);
                lastPublishCredentialsInstance = instance.PlainName;
                logger.WriteVerbose($"Kudu publish credentials for {instance.PlainName} cached.");
            }
            else
            {
                logger.WriteVerbose($"Using cached Kudu publish credentials for {instance.PlainName}.");
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
                JWT = await result.Content.ReadAsStringAsync(cancellationToken); //get  JWT for call function key
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

        public class ListingEntry
        {
            public string name { get; set; }
            public string href { get; set; }
        }

        /*
         * This code is used only in Integration tests
         */
        internal async Task<string> ReadApplicationLogAsync(string functionName, int logIndex, CancellationToken cancellationToken)
        {
            const string FunctionLogPath = "api/vfs/LogFiles/Application/Functions/Function";

            logger.WriteVerbose($"Listing application logs for {functionName}");
            using var client = new HttpClient();
            ListingEntry[] listingResult = null;

            string delayList = Environment.GetEnvironmentVariable("AGGREGATOR_KUDU_LOGRETRIEVE_ATTEMPTS")
                ?? "0:0:5 0:0:12 0:0:25 0:0:55 0:1:30";
            var delay = delayList.Split(' ').Select(s => TimeSpan.Parse(s)).ToList();
            for (int attempt = 0; attempt < delay.Count; attempt++)
            {
                logger.WriteVerbose($"Attempt #{attempt + 1} to retrieve listing");
                using var listingRequest = await GetRequestAsync(HttpMethod.Get, $"{FunctionLogPath}/{functionName}/", cancellationToken);
                var listingResponse = await client.SendAsync(listingRequest, cancellationToken);
                var listingStream = await listingResponse.Content.ReadAsStreamAsync(cancellationToken);
                if (listingResponse.IsSuccessStatusCode)
                {
                    logger.WriteVerbose($"Got list of logs, deserializing response");
                    listingResult = await JsonSerializer.DeserializeAsync<ListingEntry[]>(listingStream, cancellationToken: cancellationToken);
                    logger.WriteVerbose($"List of logs retrieved");
                    break;
                }
                else
                {
                    logger.WriteWarning($"Cannot get listing for {functionName} (attempt #{attempt + 1}): {listingResponse.ReasonPhrase}. Waiting {delay[attempt]}");
                    await Task.Delay(delay[attempt], cancellationToken);
                }
            }

            if (listingResult is null)
            {
                logger.WriteVerbose("No logs available");
                return String.Empty;
            }

            if (logIndex < 0) logIndex = listingResult.Length - 1;
            logger.WriteVerbose($"Retrieving log #{logIndex}");
            string logName = listingResult[logIndex].name;

            logger.WriteVerbose($"Retrieving log '{logName}'");
            using var logRequest = await GetRequestAsync(HttpMethod.Get, $"{FunctionLogPath}/{functionName}/{logName}", cancellationToken);
            var logResponse = await client.SendAsync(logRequest, cancellationToken);
            if (!logResponse.IsSuccessStatusCode)
            {
                logger.WriteError($"Cannot list {functionName}'s {logName} log: {logResponse.ReasonPhrase}");
                return null;
            }
            string logData = await logResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.WriteVerbose($"Log data retrieved");
            return logData;
        }

        internal async Task StreamLogsAsync(TextWriter output, string lastLinePattern, CancellationToken cancellationToken)
        {
            var regex = new Regex(lastLinePattern);

            // see https://github.com/projectkudu/kudu/wiki/Diagnostic-Log-Stream
            using var client = new HttpClient();
            using var request = await GetRequestAsync(HttpMethod.Get, $"api/logstream/application", cancellationToken);
            logger.WriteInfo($"Connected to {instance.PlainName} logs");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            logger.WriteVerbose($"Streaming {instance.PlainName} logs...");
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var reader = new StreamReader(stream);
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
