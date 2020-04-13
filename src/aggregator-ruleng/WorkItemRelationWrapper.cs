using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapper
    {
        private readonly WorkItemRelation _relation;

        private WorkItemRelationWrapper(string relationUrl)
        {
            if (!string.IsNullOrWhiteSpace(relationUrl))
            {
                var relationUri = new Uri(relationUrl);
                var idName = relationUri.Segments.Last();
                var id = int.TryParse(idName, out var i) ? i : (int?)null;
                LinkedId = id == null ? null : new PermanentWorkItemId(id.Value);
            }
        }

        internal WorkItemRelationWrapper(WorkItemRelation relation) : this(relation.Url)
        {
            _relation = relation;
        }


        internal WorkItemRelationWrapper(string type, string url, string comment) : this(url)
        {
            _relation = new WorkItemRelation()
            {
                Rel = type,
                Url = url,
                Attributes = new Dictionary<string,object> { { "comment", comment } }
            };
        }

        public string Title => _relation.Title;

        public string Rel => _relation.Rel;

        public string Url => _relation.Url;

        public WorkItemId LinkedId { get; }

        public IDictionary<string, object> Attributes => _relation.Attributes;


        //TODO: quick filter for work item relations only
        internal static bool IsWorkItemRelation(WorkItemRelation relation) => IsWorkItemRelation(relation.Url);
        internal static bool IsWorkItemRelation(RelationPatch relationPatch) => IsWorkItemRelation(relationPatch.url);
        private static bool IsWorkItemRelation(string url) => url?.Contains("/_apis/wit/workItems/", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
