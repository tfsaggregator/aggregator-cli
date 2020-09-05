using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator
{
    public sealed class AzureDevOpsClientsContext : IDisposable, IClientsContext
    {
        private readonly VssConnection _vssConnection;
        private readonly IDictionary<Type, VssHttpClientBase> _resources;

        public AzureDevOpsClientsContext(VssConnection connection)
        {
            _vssConnection = connection;
            _resources = new Dictionary<Type, VssHttpClientBase>();
        }

        public WorkItemTrackingHttpClient WitClient => GetClientFromResource<WorkItemTrackingHttpClient>();
        public WorkHttpClient WorkClient => GetClientFromResource<WorkHttpClient>();

        private T GetClientFromResource<T>() where T : VssHttpClientBase
        {
            if (_resources.ContainsKey(typeof(T)))
            {
                return (T)_resources[typeof(T)];
            }

            var client = _vssConnection.GetClient<T>();
            _resources[typeof(T)] = client;
            return client;
        }

        public void Dispose()
        {
            foreach (var httpClient in _resources.Values)
            {
                httpClient.Dispose();
            }
        }
    }
}
