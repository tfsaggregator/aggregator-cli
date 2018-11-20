using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.Engine
{
    public class WorkItemStore
    {
        private EngineContext _context;

        public WorkItemStore(EngineContext context)
        {
            _context = context;
        }

        public WorkItemWrapper GetWorkItem(int id)
        {
            _context.Logger.WriteVerbose($"Getting workitem {id}");

            return _context.Tracker.LoadWorkItem(id, (workItemId) =>
                {
                    _context.Logger.WriteInfo($"Loading workitem {workItemId}");
                    var item = _context.Client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Result;
                    return new WorkItemWrapper(_context, item);
                });
        }

        public WorkItemWrapper GetWorkItem(WorkItemRelationWrapper item)
        {
            int id = int.Parse(
                item.Url.Substring(
                    item.Url.LastIndexOf('/') + 1));
            return GetWorkItem(id);
        }

        public IList<WorkItemWrapper> GetWorkItems(IEnumerable<int> ids)
        {
            string idList = ids.Aggregate("", (s, i) => s += $",{i}");
            _context.Logger.WriteVerbose($"Getting workitems {idList.Substring(1)}");
            return _context.Tracker.LoadWorkItems(ids, (workItemIds) =>
            {
                string idList2 = workItemIds.Aggregate("", (s, i) => s += $",{i}");
                _context.Logger.WriteInfo($"Loading workitems {idList2.Substring(1)}");
                var items = _context.Client.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All).Result;
                return items.ConvertAll(i => new WorkItemWrapper(_context, i));
            });
        }

        public IList<WorkItemWrapper> GetWorkItems(IEnumerable<WorkItemRelationWrapper> collection)
        {
            var ids = new List<int>();
            foreach (var item in collection)
            {
                ids.Add(
                    int.Parse(
                        item.Url.Substring(
                            item.Url.LastIndexOf('/') + 1)));
            }

            return GetWorkItems(ids);
        }

        public WorkItemWrapper NewWorkItem(string workItemType)
        {
            var item = new WorkItem()
            {
                Fields = new Dictionary<string, object>() {
                    { CoreFieldRefNames.WorkItemType, workItemType },
                    { CoreFieldRefNames.TeamProject, _context.ProjectId.ToString() }
                },
                Relations = new List<WorkItemRelation>(),
                Links = new Microsoft.VisualStudio.Services.WebApi.ReferenceLinks()
            };
            var wrapper = new WorkItemWrapper(_context, item);
            _context.Logger.WriteVerbose($"Made new workitem with temporary id {wrapper.Id.Value}");
            //HACK
            string baseUriString = _context.Client.BaseAddress.AbsoluteUri;
            item.Url = $"{baseUriString}/_apis/wit/workitems/{wrapper.Id.Value}";
            return wrapper;
        }

        public async Task<(int created, int updated)> SaveChanges(SaveMode mode, bool commit)
        {
            switch (mode)
            {
                case SaveMode.Default:
                    _context.Logger.WriteVerbose($"No save mode specified, assuming {SaveMode.TwoPhases}.");
                    goto case SaveMode.TwoPhases;
                case SaveMode.Item:
                    return await SaveChanges_ByItem(commit);
                case SaveMode.Batch:
                    return await SaveChanges_Batch(commit);
                case SaveMode.TwoPhases:
                    return await SaveChanges_TwoPhases(commit);
                default:
                    throw new ApplicationException($"Unsupported save mode: {mode}.");
            }
        }

        private async Task<(int created, int updated)> SaveChanges_ByItem(bool commit)
        {
            int created = 0;
            int updated = 0;
            foreach (var item in _context.Tracker.NewWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Creating a {item.WorkItemType} workitem in {_context.ProjectId}");
                    var wi = await _context.Client.CreateWorkItemAsync(
                        item.Changes,
                        _context.ProjectId,
                        item.WorkItemType
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should create a {item.WorkItemType} workitem in {_context.ProjectId}");
                }
                created++;
            }

            foreach (var item in _context.Tracker.ChangedWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Updating workitem {item.Id}");
                    var wi = await _context.Client.UpdateWorkItemAsync(
                        item.Changes,
                        item.Id.Value
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should update workitem {item.Id} in {_context.ProjectId}");
                }
                updated++;
            }
            return (created, updated);
        }

        private async Task<(int created, int updated)> SaveChanges_Batch(bool commit)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // BUG this code won't work if there is a relation between a new (id<0) work item and an existing one (id>0): it is an API limit

            const string ApiVersion = "api-version=4.1";

            int created = _context.Tracker.NewWorkItems.Count();
            int updated = _context.Tracker.ChangedWorkItems.Count();

            string baseUriString = _context.Client.BaseAddress.AbsoluteUri;

            BatchRequest[] batchRequests = new BatchRequest[created + updated];
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Content-Type", "application/json-patch+json" }
            };
            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{_context.PersonalAccessToken}"));

            int index = 0;

            foreach (var item in _context.Tracker.NewWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {_context.ProjectName}");

                batchRequests[index++] = new BatchRequest
                {
                    method = "PATCH",
                    uri = $"/{_context.ProjectName}/_apis/wit/workitems/${item.WorkItemType}?{ApiVersion}",
                    headers = headers,
                    body = item.Changes
                        .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test)
                        .ToArray()
                };
            }
            foreach (var item in _context.Tracker.ChangedWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id.Value} in {_context.ProjectName}");

                batchRequests[index++] = new BatchRequest
                {
                    method = "PATCH",
                    uri = $"/_apis/wit/workitems/{item.Id.Value}?{ApiVersion}",
                    headers = headers,
                    body = item.Changes
                        .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test)
                        .ToArray()
                };
            }
            var converters = new JsonConverter[] { new JsonPatchOperationConverter() };
            string requestBody = JsonConvert.SerializeObject(batchRequests, Formatting.Indented, converters);
            _context.Logger.WriteVerbose(requestBody);

            if (commit)
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
                            throw new ApplicationException($"Save failed.");
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
            }//if

            return (created, updated);
        }

        private async Task<(int created, int updated)> SaveChanges_TwoPhases(bool commit)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // The workitembatchupdate API has a huge limit:
            // it fails adding a relation between a new (id<0) work item and an existing one (id>0)

            const string ApiVersion = "api-version=4.1";
            string baseUriString = _context.Client.BaseAddress.AbsoluteUri;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Content-Type", "application/json-patch+json" }
            };
            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{_context.PersonalAccessToken}"));
            var converters = new JsonConverter[] { new JsonPatchOperationConverter() };

            int created = _context.Tracker.NewWorkItems.Count();
            int updated = _context.Tracker.ChangedWorkItems.Count();

            BatchRequest[] newWorkItemsBatchRequests = new BatchRequest[created];
            int index = 0;
            foreach (var item in _context.Tracker.NewWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {_context.ProjectName}");

                newWorkItemsBatchRequests[index++] = new BatchRequest
                {
                    method = "PATCH",
                    uri = $"/{_context.ProjectName}/_apis/wit/workitems/${item.WorkItemType}?{ApiVersion}",
                    headers = headers,
                    body = item.Changes
                        .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test)
                        // remove relations as we might incour in API failure
                        .Where(c => c.Path != "/relations/-")
                        .ToArray()
                };
            }
            string requestBody = JsonConvert.SerializeObject(newWorkItemsBatchRequests, Formatting.Indented, converters);
            _context.Logger.WriteVerbose($"New workitem(s) batch request:");
            _context.Logger.WriteVerbose(requestBody);

            if (commit)
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
                        _context.Logger.WriteVerbose($"New workitem(s) batch response:");
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
                        else
                        {
                            _context.Logger.WriteVerbose($"Updating work item ids...");
                            // Fix back
                            var realIds = new Dictionary<int, int>();
                            index = 0;
                            foreach (var item in _context.Tracker.NewWorkItems)
                            {
                                int oldId = item.Id.Value;
                                // the response order matches the request order
                                string createdWorkitemJson = batchResponse.values[index++].body;
                                dynamic createdWorkitemResult = JsonConvert.DeserializeObject(createdWorkitemJson);
                                int newId = createdWorkitemResult.id;
                                item.ReplaceIdAndResetChanges(item.Id.Value, newId);
                                realIds.Add(oldId, newId);
                            }
                            foreach (var item in _context.Tracker.ChangedWorkItems)
                            {
                                item.RemapIdReferences(realIds);
                            }
                        }
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
            }//if

            var batchRequests = new List<BatchRequest>();
            var allWorkItems = _context.Tracker.NewWorkItems.Concat(_context.Tracker.ChangedWorkItems);
            foreach (var item in allWorkItems)
            {
                var changes = item.Changes
                        .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test);
                if (changes.Any())
                {
                    _context.Logger.WriteInfo($"Found a request to update workitem {item.Id.Value} in {_context.ProjectName}");

                    batchRequests.Add(new BatchRequest
                    {
                        method = "PATCH",
                        uri = $"/_apis/wit/workitems/{item.Id.Value}?{ApiVersion}",
                        headers = headers,
                        body = changes.ToArray()
                    });
                }
            }

            requestBody = JsonConvert.SerializeObject(batchRequests.ToArray(), Formatting.Indented, converters);
            _context.Logger.WriteVerbose($"Update workitem(s) batch request:");
            _context.Logger.WriteVerbose(requestBody);

            if (commit)
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
                            throw new ApplicationException($"Save failed.");
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
            }//if

            return (created, updated);
        }
    }


    class JsonPatchOperationConverter : JsonConverter<Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation>
    {
        public override bool CanRead => false;

        public override JsonPatchOperation ReadJson(JsonReader reader, Type objectType, JsonPatchOperation existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override void WriteJson(JsonWriter writer, JsonPatchOperation value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value);

            if (t.Type != JTokenType.Object)
            {
                t.WriteTo(writer);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName("op");
                writer.WriteValue(value.Operation.ToString().ToLower());
                writer.WritePropertyName("path");
                writer.WriteValue(value.Path);
                if (!string.IsNullOrEmpty(value.From))
                {
                    writer.WritePropertyName("from");
                    writer.WriteValue(value.From);
                }
                writer.WritePropertyName("value");
                t = JToken.FromObject(value.Value);
                t.WriteTo(writer);
                writer.WriteEndObject();
            }
        }
    }
}