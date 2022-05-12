using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using aggregator.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator
{
    public class RequestHelper
    {
        private readonly ILogger _log;

        public RequestHelper(ILogger logger)
        {
            _log = logger;
        }

        public WorkItemEventContext CreateContextFromEvent(WebHookEvent eventData)
        {
            var collectionUrl = eventData.ResourceContainers.GetValueOrDefault("collection")?.BaseUrl ?? MagicConstants.MissingUrl;
            var teamProjectId = eventData.ResourceContainers.GetValueOrDefault("project")?.Id ?? Guid.Empty;

            if (eventData.Resource is Newtonsoft.Json.Linq.JObject)
            {
                // Azure Function uses Newtonsoft.Json
                return Newtonsoft_CreateContextFromEvent(eventData, collectionUrl, teamProjectId);
            }
            else
            {
                // ASP.Net uses System.Text.Json
                return SystemTextJson_CreateContextFromEvent(eventData, collectionUrl, teamProjectId);
            }

        }

#pragma warning disable CA1707 // Identifiers should not contain underscores
        private WorkItemEventContext Newtonsoft_CreateContextFromEvent(WebHookEvent eventData, string collectionUrl, Guid teamProjectId)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            var resourceObject = eventData.Resource as Newtonsoft.Json.Linq.JObject;
            if (ServiceHooksEventTypeConstants.WorkItemUpdated == eventData.EventType)
            {
                var workItem = resourceObject.GetValue("revision").ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                var workItemUpdate = resourceObject.ToObject<WorkItemUpdate>();
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, eventData.EventType, workItemUpdate);
            }
            else
            {
                var workItem = resourceObject.ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, eventData.EventType);
            }
        }

#pragma warning disable CA1707 // Identifiers should not contain underscores
        public WorkItemEventContext SystemTextJson_CreateContextFromEvent(WebHookEvent eventData, string collectionUrl, Guid teamProjectId)
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            var resourceObject = (JsonElement)eventData.Resource;
            if (ServiceHooksEventTypeConstants.WorkItemUpdated == eventData.EventType)
            {
                var workItem = resourceObject.GetProperty("revision").ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                var workItemUpdate = resourceObject.ToObject<WorkItemUpdate>();
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, eventData.EventType, workItemUpdate);
            }
            else
            {
                var workItem = resourceObject.ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, eventData.EventType);
            }
        }


        /// <summary>
        /// in Event Resource Version == 1.0 the Identity Information is provided in a single string "DisplayName <UniqueName>", in newer Revisions
        /// the Identity Information as on Object of type IdentityRef
        /// As we rely on IdentityRef we could switch the WebHook to use ResourceVersion > 1.0 but unfortunately there is a bug
        /// as these Resources do not send the relation information in the event (although resource details is set to all).
        /// Option 1: Use Resource Version > 1.0 and load work item later to get relation information
        /// Option 2: Use Resource Version == 1.0 and convert string to IdentityRef
        ///
        /// Use Option 2, as less server round trips, write warning in case of too new Resource Version, Open ticket at Microsoft and see if they accept it as Bug.
        /// </summary>
        /// <param name="resourceVersion"></param>
        /// <param name="workItem"></param>
        public void MigrateIdentityInformation(string resourceVersion, WorkItem workItem)
        {
            const char UNIQUE_NAME_START_CHAR = '<';
            const char UNIQUE_NAME_END_CHAR = '>';

            if (!resourceVersion.StartsWith("1.0"))
            {
                _log.LogWarning($"Mapping is using Resource Version {resourceVersion}, which can lead to some issues with e.g. not available relation information on trigger work item.");
                return;
            }

            static IdentityRef ConvertOrDefault(string input)
            {
                var uniqueNameStartIndex = input.LastIndexOf(UNIQUE_NAME_START_CHAR);
                var uniqueNameEndIndex = input.LastIndexOf(UNIQUE_NAME_END_CHAR);

                if (uniqueNameStartIndex < 0 || uniqueNameEndIndex != input.Length - 1)
                {
                    return null;
                }

                return new IdentityRef()
                {
                    DisplayName = input[0..uniqueNameStartIndex].Trim(),
                    UniqueName = input[(uniqueNameStartIndex + 1)..uniqueNameEndIndex]
                };
            }

            // assumtion to get all string Identity Fields, normally the naming convention is: These fields ends with TO or BY (e.g. AssignedTO, CreatedBY)
            var identityFieldReferenceNameEndings = new[]
                                 {
                                     "By", "To"
                                 };

            foreach (var identityField in workItem.Fields.Where(field => identityFieldReferenceNameEndings.Any(name => field.Key.EndsWith(name))).ToList())
            {
                IdentityRef identityRef = identityField.Value switch
                {
                    string identityString => ConvertOrDefault(identityString),
                    JsonElement identityElement => ConvertOrDefault(identityElement.GetString()),
                    _ => null
                };
                if (identityRef != null)
                {
                    workItem.Fields[identityField.Key] = identityRef;
                }
            }
        }

        public static string GetTestEventMessageReply(string aggregatorVersion, string ruleName)
        {
            return $"Hello from Aggregator v{aggregatorVersion} executing rule '{ruleName}'";
        }

        public static string AggregatorVersion => RequestHelper.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        private static T GetCustomAttribute<T>()
            where T : Attribute
        {
            return System.Reflection.Assembly
                         .GetExecutingAssembly()
                         .GetCustomAttributes(typeof(T), false)
                         .FirstOrDefault() as T;
        }
    }
}
