using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace aggregator.Engine
{
    class BatchProxy
    {
        private readonly bool _commit;
        private readonly EngineContext _context;

        internal BatchProxy(EngineContext context, bool commit)
        {
            this._context = context;
            this._commit = commit;
        }

        internal string ApiVersion => "api-version=4.1";

        internal async Task<WorkItemBatchPostResponse> Invoke(BatchRequest[] batchRequests)
        {
            string baseUriString = _context.Client.BaseAddress.AbsoluteUri;
            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{_context.PersonalAccessToken}"));
            var converters = new JsonConverter[] { new JsonPatchOperationConverter() };

            string requestBody = JsonConvert.SerializeObject(batchRequests, Formatting.Indented, converters);
            _context.Logger.WriteVerbose($"Workitem(s) batch request:");
            _context.Logger.WriteVerbose(requestBody);

            if (_commit)
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                    var batchRequest = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    var method = new HttpMethod("POST");

                    // send the request
                    var request = new HttpRequestMessage(method, $"{baseUriString}/_apis/wit/$batch?{ApiVersion}") { Content = batchRequest };
                    var response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        WorkItemBatchPostResponse batchResponse = response.Content.ReadAsAsync<WorkItemBatchPostResponse>().Result;

                        string stringResponse = JsonConvert.SerializeObject(batchResponse, Formatting.Indented);
                        _context.Logger.WriteVerbose($"Workitem(s) batch response:");
                        _context.Logger.WriteVerbose(stringResponse);

                        bool succeeded = true;
                        foreach (var batchElement in batchResponse.values)
                        {
                            if (batchElement.code != 200)
                            {
                                _context.Logger.WriteError($"Save failed: {batchElement.body}");
                                succeeded = false;
                            }
                        }
                        if (!succeeded)
                        {
                            throw new ApplicationException($"Save failed.");
                        }

                        return batchResponse;
                    }
                    else
                    {
                        string stringResponse = await response.Content.ReadAsStringAsync();
                        _context.Logger.WriteError($"Save failed: {stringResponse}");
                        throw new ApplicationException($"Save failed: {response.ReasonPhrase}.");
                    }
                }//using
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
                return null;
            }//if
        }
    }
}
