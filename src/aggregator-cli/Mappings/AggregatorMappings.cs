using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;

namespace aggregator.cli
{
    internal class AggregatorMappings
    {
        private VssConnection vsts;

        public AggregatorMappings(VssConnection vsts)
        {
            this.vsts = vsts;
        }

        internal IEnumerable<(string rule, string project, string events)> List()
        {
            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = serviceHooksClient
                .QuerySubscriptionsAsync()
                .Result
                .Where(s
                    => s.PublisherId == "tfs"
                    && s.EventType == "workitem.created"
                    //&& s.ConsumerId == "webHooks"
                    //&& s.ConsumerActionId == "httpRequest"
                    );

            foreach (var subscription in subscriptions)
            {
                yield return (
                    subscription.ActionDescription,
                    subscription.ActionDescription,
                    subscription.EventType
                    );
            }
        }
    }
}